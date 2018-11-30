using System;
using System.Collections.Generic;
using System.IO;
using Shipwreck.Web.Rfc;

#if ASPNET_CORE
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
#else

using System.Web;
using System.Web.Mvc;

#endif

#if ASPNET_CORE
namespace Shipwreck.AspNetCore
#else

namespace Shipwreck.AspNet.Mvc
#endif
{
    /// <summary>
    ///  <see cref="Stream"/>インスタンスを使用して、バイナリ コンテンツの指定されたバイトの範囲を応答に送信します。
    /// </summary>
    /// <seealso href="https://httpwg.org/specs/rfc7233.html">
    /// RFC 7233 Hypertext Transfer Protocol (HTTP/1.1): Range Requests
    /// </seealso>
    public class ByteRangeStreamResult
#if ASPNET_CORE
        : IActionResult
#else
        : ActionResult
#endif
    {
        /// <summary>
        /// 応答に送信するストリームです。
        /// </summary>
        private readonly Stream _Stream;

        /// <summary>
        /// 応答に送信するファイルとコンテンツ タイプを指定して<see cref="ByteRangeStreamResult" />クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="fileName">応答に送信するファイルの名前。</param>
        /// <param name="contentType">応答に使用するコンテンツ タイプ。</param>
        public ByteRangeStreamResult(string fileName, string contentType)
            : this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read), contentType)
        {
        }

        /// <summary>
        /// 応答に送信するストリームとコンテンツ タイプを指定して<see cref="ByteRangeStreamResult" />クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="stream">応答に送信するストリーム。</param>
        /// <param name="contentType">応答に使用するコンテンツ タイプ。</param>
        public ByteRangeStreamResult(Stream stream, string contentType)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException($"{nameof(stream)}で読み取りおよびシークがサポートされていません。");
            }

            _Stream = stream;
            ContentType = contentType;
        }

        /// <summary>
        /// 応答に使用するコンテンツ タイプを取得します。
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// ファイル ダウンロード ダイアログ ボックスが、指定されたファイル名と共にブラウザーに表示されるように、Content-Disposition ヘッダーを取得または設定します。
        /// </summary>
        public string FileDownloadName { get; set; }

        /// <summary>
        /// バイトの範囲を送信するかどうかを示す値を取得または設定します。
        /// </summary>
        public bool AcceptRanges { get; set; } = true;

        /// <summary>
        /// クライアント側の切断を無視するかどうかを示す値を取得または設定します。
        /// </summary>
        public bool IgnoreDisconnection { get; set; }

        public string ETag { get; set; }

        public DateTime? LastModified { get; set; }

#if ASPNET_CORE
        public async Task ExecuteResultAsync(ActionContext context)
#else

        /// <inheritdoc />
        public override void ExecuteResult(ControllerContext context)
#endif
        {
            using (_Stream)
            {
                var res = context.HttpContext.Response;

                var rh = context.HttpContext.Request.Headers["Range"];

                List<ByteRange> ranges;

                var start = -1L;
                var length = -1L;
                if (AcceptRanges && ByteRange.TryParseHeader(rh, out ranges) && ranges.Count == 1)
                {
                    // Single byte range

                    var r = ranges[0];

                    if (r.IsSuffixLength)
                    {
                        length = r.SuffixLength;
                        start = _Stream.Length - length;
                    }
                    else
                    {
                        start = r.FirstIndex;
                        length = (r.LastIndex + 1 ?? _Stream.Length) - start;
                    }

                    if (0 > start || start >= _Stream.Length
                        || length <= 0 || start + length > _Stream.Length)
                    {
                        start = -1;
                    }
                }

                if (start >= 0)
                {
                    res.StatusCode = 206;
#if !ASPNET_CORE
                    res.StatusDescription = "Partial Content";
#endif
                    res.ContentType = ContentType ?? "application/octet-stream";
                    res.Headers["Content-Length"] = length.ToString();
                    res.Headers["Content-Range"] = $"bytes {start}-{start + length - 1}/{_Stream.Length}";
                }
                else
                {
                    // No Range Specified or Not Supported format
                    res.StatusCode = 200;
#if !ASPNET_CORE
                    res.StatusDescription = "OK";
#endif
                    res.ContentType = ContentType ?? "application/octet-stream";
                    res.Headers["Content-Length"] = _Stream.Length.ToString();
                    if (FileDownloadName != null)
                    {
                        res.Headers["Content-Disposition"] = Rfc6266.GetContentDispositionHeader(FileDownloadName);
                    }
                    start = 0;
                    length = _Stream.Length;
                }
                if (AcceptRanges)
                {
                    res.Headers["Accept-Ranges"] = "bytes";
                }

                if (ETag?.Length > 0)
                {
                    res.Headers["ETag"] = "\"" + ETag + "\"";
                }

                if (LastModified != null)
                {
                    res.Headers["Last-Modified"] = LastModified.Value.ToString("R");
                }

#if ASPNET_CORE
                if (context.HttpContext.Request.Method == "HEAD")
#else
                if (context.HttpContext.Request.HttpMethod == "HEAD")
#endif
                {
                    return;
                }
                var buff = new byte[Math.Min(length, 4096)];

                _Stream.Position = start;

                try
                {
#if ASPNET_CORE
                    await StreamCopyOperation.CopyToAsync(_Stream, context.HttpContext.Response.Body, length, context.HttpContext.RequestAborted).ConfigureAwait(false);
#else
                    while (res.IsClientConnected && length > 0)
                    {
                        var l = _Stream.Read(buff, 0, (int)Math.Min(length, buff.Length));
                        if (l <= 0)
                        {
                            break;
                        }
                        length -= l;
                        res.OutputStream.Write(buff, 0, l);
                        res.Flush();
                    }
#endif
                }
#if ASPNET_CORE
                catch
                {
#else
                catch (HttpException ex)
                {
                    if (IgnoreDisconnection
                        && ex.ErrorCode == unchecked((int)0x800703E3))
                    {
                        return;
                    }
#endif
                    throw;
                }
            }
        }
    }
}