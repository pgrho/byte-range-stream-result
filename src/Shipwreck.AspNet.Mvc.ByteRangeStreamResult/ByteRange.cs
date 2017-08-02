using System;
using System.Collections.Generic;

#if ASPNET_CORE
namespace Shipwreck.AspNetCore
#else
namespace Shipwreck.AspNet.Mvc
#endif
{
    /// <summary>
    /// Represents a byte range of HTTP Range header.
    /// </summary>
    public struct ByteRange : IEquatable<ByteRange>
    {
        #region Fields

        private readonly int _FirstIndex;

        private readonly int _LastIndexOrSuffixLength;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// 開始バイトと終了バイトの<c>0</c>から始まるインデックスを指定して<see cref="ByteRange"/>構造体の新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="firstIndex">開始バイトの<c>0</c>から始まるインデックス。</param>
        /// <param name="lastIndexOrNegative">終了バイトの<c>0</c>から始まるインデックス。終了バイトを指定しない場合は負の値。</param>
        public ByteRange(int firstIndex, int lastIndexOrNegative)
        {
            if (firstIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstIndex));
            }

            _FirstIndex = firstIndex;

            if (lastIndexOrNegative < 0)
            {
                _LastIndexOrSuffixLength = -1;
            }
            else
            {
                if (lastIndexOrNegative < firstIndex)
                {
                    throw new ArgumentOutOfRangeException(nameof(lastIndexOrNegative));
                }
                else
                {
                    _LastIndexOrSuffixLength = lastIndexOrNegative;
                }
            }
        }

        /// <summary>
        /// 送信する範囲のファイル末尾からのバイト数を指定して<see cref="ByteRange"/>構造体の新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="suffixLength">送信するファイル末尾からのバイト数。</param>
        public ByteRange(int suffixLength)
        {
            if (suffixLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(suffixLength));
            }

            _FirstIndex = -1;
            _LastIndexOrSuffixLength = suffixLength;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// 現在の範囲が末尾からのバイト数指定であるかどうかを示す値を取得します。
        /// </summary>
        public bool IsSuffixLength => _FirstIndex < 0;

        /// <summary>
        /// 現在の範囲の<c>0</c>から始まる開始バイトを取得します。
        /// </summary>
        /// <exception cref="InvalidOperationException">現在の範囲が末尾からのバイト数指定である場合。</exception>
        public int FirstIndex
        {
            get
            {
                if (IsSuffixLength)
                {
                    throw new InvalidOperationException();
                }
                return _FirstIndex;
            }
        }

        /// <summary>
        /// 現在の範囲の<c>0</c>から始まる終了バイトを取得します。
        /// </summary>
        /// <exception cref="InvalidOperationException">現在の範囲が末尾からのバイト数指定である場合。</exception>
        public int? LastIndex
        {
            get
            {
                if (IsSuffixLength)
                {
                    throw new InvalidOperationException();
                }
                if (_LastIndexOrSuffixLength < 0)
                {
                    return null;
                }
                return _LastIndexOrSuffixLength;
            }
        }

        /// <summary>
        /// 現在の範囲のファイル末尾からのバイト数を取得します。
        /// </summary>
        /// <exception cref="InvalidOperationException">現在の範囲が末尾からのバイト数指定でない場合。</exception>
        public int SuffixLength
        {
            get
            {
                if (!IsSuffixLength)
                {
                    throw new InvalidOperationException();
                }
                return _LastIndexOrSuffixLength;
            }
        }

        #endregion Properties

        #region Methods

        #region Operator Overloads

        /// <summary>
        /// 2つのバイトの範囲が等しいかどうかを示す値を返します。
        /// </summary>
        /// <param name="left">比較対象の最初のバイトの範囲。</param>
        /// <param name="right">比較対象の2番目のバイトの範囲。</param>
        /// <returns><paramref name="left" />と<paramref name="right" />が同じ値の場合は<c>true</c>。それ以外の場合は<c>false</c>。</returns>
        public static bool operator ==(ByteRange left, ByteRange right)
            => left._FirstIndex == right._FirstIndex && left._LastIndexOrSuffixLength == right._LastIndexOrSuffixLength;

        /// <summary>
        /// 2つのバイトの範囲が等しくないかどうかを示す値を返します。
        /// </summary>
        /// <param name="left">比較対象の最初のバイトの範囲。</param>
        /// <param name="right">比較対象の2番目のバイトの範囲。</param>
        /// <returns><paramref name="left" />と<paramref name="right" />が異なる値の場合は<c>true</c>。それ以外の場合は<c>false</c>。</returns>
        public static bool operator !=(ByteRange left, ByteRange right)
            => left._FirstIndex != right._FirstIndex || left._LastIndexOrSuffixLength != right._LastIndexOrSuffixLength;

        #endregion Operator Overloads

        #region Range Parsing Methods

        /// <summary>
        /// バイトの範囲の文字列形式をそれと等価な<see cref="ByteRange" />に変換します。
        /// </summary>
        /// <param name="s">変換するバイトの範囲を格納する文字列。</param>
        /// <returns><paramref name="s"/>に格納されたバイトの範囲と等しい<see cref="ByteRange"/>。</returns>
        public static ByteRange Parse(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            ByteRange r;
            if (!TryParse(s, out r))
            {
                throw new FormatException();
            }
            return r;
        }

        /// <summary>
        /// バイトの範囲の文字列形式をそれと等価な<see cref="ByteRange" />に変換します。戻り値は変換が成功したかどうかを示します。
        /// </summary>
        /// <param name="s">変換するバイトの範囲を格納する文字列。</param>
        /// <param name="result">
        /// 変換が成功した場合、<paramref name="s"/>に格納されたバイトの範囲と等しい<see cref="ByteRange"/>を格納します。
        /// 変換に失敗した場合は<see cref="ByteRange"/>の既定値を格納します。</param>
        /// <returns><paramref name="s"/>が正常に変換された場合は<c>true</c>。それ以外の場合は<c>false</c>。</returns>
        public static bool TryParse(string s, out ByteRange result)
        {
            if (s == null)
            {
                result = default(ByteRange);
                return false;
            }

            return TryParse(s, 0, s.Length, out result);
        }

        /// <summary>
        /// 文字列の一部分に格納されたバイトの範囲の文字列形式をそれと等価な<see cref="ByteRange" />に変換します。戻り値は変換が成功したかどうかを示します。
        /// </summary>
        /// <param name="s">変換するバイトの範囲を格納する文字列。</param>
        /// <param name="startIndex">変換範囲の最初の文字の<c>0</c>から始まるインデックス。</param>
        /// <param name="length">変換範囲の文字数。</param>
        /// <param name="result">
        /// 変換が成功した場合、<paramref name="s"/>の指定された範囲に格納されたバイトの範囲と等しい<see cref="ByteRange"/>を格納します。
        /// 変換に失敗した場合は<see cref="ByteRange"/>の既定値を格納します。</param>
        /// <returns><paramref name="s"/>の指定された範囲が正常に変換された場合は<c>true</c>。それ以外の場合は<c>false</c>。</returns>
        public static bool TryParse(string s, int startIndex, int length, out ByteRange result)
        {
            if (s == null)
            {
                result = default(ByteRange);
                return false;
            }
            var mi = s.IndexOf('-', startIndex);
            var ci = startIndex + length;

            if (startIndex < 0 || length <= 0 || startIndex + length - 1 >= s.Length || mi < -1 || mi > ci)
            {
                result = default(ByteRange);
                return false;
            }
            else if (mi == startIndex)
            {
                int sl;
                if (!TryParseInt32(s, startIndex + 1, ci - startIndex - 1, out sl) || sl <= 0)
                {
                    result = default(ByteRange);
                    return false;
                }
                result = new ByteRange(sl);
                return true;
            }
            else if (mi == ci - 1)
            {
                int sl;
                if (!TryParseInt32(s, startIndex, ci - startIndex - 1, out sl))
                {
                    result = default(ByteRange);
                    return false;
                }
                result = new ByteRange(sl, -1);
                return true;
            }
            else
            {
                int f, l;
                if (!TryParseInt32(s, startIndex, mi - startIndex, out f)
                    || !TryParseInt32(s, mi + 1, ci - mi - 1, out l)
                    || f > l)
                {
                    result = default(ByteRange);
                    return false;
                }
                result = new ByteRange(f, l);
                return true;
            }
        }

        private static bool TryParseInt32(string s, int startIndex, int length, out int result)
        {
            if (startIndex < 0 || length <= 0 || startIndex + length - 1 >= s.Length)
            {
                result = 0;
                return false;
            }

            var b = 0L;
            for (var i = 0; i < length; i++)
            {
                var c = s[startIndex + i];
                if ('0' <= c && c <= '9')
                {
                    b = b * 10 + c - '0';
                    if (b > int.MaxValue)
                    {
                        result = 0;
                        return false;
                    }
                }
                else
                {
                    result = 0;
                    return false;
                }
            }

            result = (int)b;
            return true;
        }

        #endregion Range Parsing Methods

        #region Header Parsing Methods

        /// <summary>
        /// HTTP <c>Range</c>ヘッダーの値をそれと等価な<see cref="ByteRange" />のリストに変換します。
        /// </summary>
        /// <param name="headerValue">変換するHTTP <c>Range</c>ヘッダーを格納する文字列</param>
        /// <returns><paramref name="headerValue"/>に格納されたバイトの範囲のリスト。</returns>
        public static List<ByteRange> ParseHeader(string headerValue)
        {
            if (headerValue == null)
            {
                throw new ArgumentNullException(nameof(headerValue));
            }
            List<ByteRange> r;
            if (!TryParseHeader(headerValue, out r))
            {
                throw new FormatException();
            }
            return r;
        }

        /// <summary>
        /// HTTP <c>Range</c>ヘッダーの値をそれと等価な<see cref="ByteRange" />のリストに変換します。戻り値は変換が成功したかどうかを示します。
        /// </summary>
        /// <param name="headerValue">変換するHTTP <c>Range</c>ヘッダーを格納する文字列</param>
        /// <param name="result">
        /// 変換が成功した場合、<paramref name="headerValue"/>に格納されたバイトの範囲のリストを格納します。
        /// 変換に失敗した場合は<c>null</c>を格納します。</param>
        /// <returns><paramref name="headerValue"/>が正常に変換された場合は<c>true</c>。それ以外の場合は<c>false</c>。</returns>

        public static bool TryParseHeader(string headerValue, out List<ByteRange> result)
        {
            var r = new List<ByteRange>(1);
            if (TryParseHeader(headerValue, r))
            {
                result = r;
                return true;
            }
            result = null;
            return false;
        }

        private static bool TryParseHeader(string headerValue, ICollection<ByteRange> result)
        {
            if (headerValue?.StartsWith("bytes=") != true)
            {
                return false;
            }

            var si = 6;

            while (si < headerValue.Length)
            {
                var ci = headerValue.IndexOf(',', si);
                ci = ci < 0 ? headerValue.Length : ci;

                ByteRange br;
                if (!TryParse(headerValue, si, ci - si, out br))
                {
                    return false;
                }

                result.Add(br);

                si = ci + 1;
            }

            return true;
        }

        #endregion Header Parsing Methods

        #region Instance Methods

        /// <inheritdoc />
        public override bool Equals(object obj)
            => obj is ByteRange && this == (ByteRange)obj;

        /// <summary>
        /// 現在のインスタンスの値と指定したバイトの範囲の値が等しいかどうかを示す値を返します。
        /// </summary>
        /// <param name="other">比較対象のバイトの範囲。</param>
        /// <returns>
        /// このバイトの範囲の値と<paramref name="other" />の値が等しい場合はtrue。
        /// それ以外の場合はfalse。
        /// </returns>
        public bool Equals(ByteRange other) => this == other;

        /// <inheritdoc />
        public override int GetHashCode()
            => unchecked((short)_FirstIndex ^ (((short)_LastIndexOrSuffixLength) << 16));

        /// <inheritdoc />
        public override string ToString()
        {
            if (IsSuffixLength)
            {
                return $"-{_LastIndexOrSuffixLength}";
            }
            if (_LastIndexOrSuffixLength < 0)
            {
                return $"{_FirstIndex}-";
            }
            return $"{_FirstIndex}-{_LastIndexOrSuffixLength}";
        }

        #endregion Instance Methods

        #endregion Methods
    }


}