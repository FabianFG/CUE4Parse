// CRC32.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2011 Dino Chiesa.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// Last Saved: <2011-August-02 18:25:54>
//
// ------------------------------------------------------------------
//
// This module defines the CRC32 class, which can do the CRC32 algorithm, using
// arbitrary starting polynomials, and bit reversal. The bit reversal is what
// distinguishes this CRC-32 used in BZip2 from the CRC-32 that is used in PKZIP
// files, or GZIP files. This class does both.
//
// ------------------------------------------------------------------


using System;
using System.IO;
using Interop = System.Runtime.InteropServices;

namespace CUE4Parse.Utils;

/// <summary>
///   Computes a CRC-32. The CRC-32 algorithm is parameterized - you
///   can set the polynomial and enable or disable bit
///   reversal. This can be used for GZIP, BZip2, or ZIP.
/// </summary>
/// <remarks>
///   This type is used internally by DotNetZip; it is generally not used
///   directly by applications wishing to create, read, or manipulate zip
///   archive files.
/// </remarks>
[Interop.Guid("ebc25cf6-9120-4283-b972-0e5520d0000C")]
[Interop.ComVisible(true)]
[Interop.ClassInterface(Interop.ClassInterfaceType.AutoDispatch)]
public class CRC32
{
    /// <summary>
    ///   Indicates the total number of bytes applied to the CRC.
    /// </summary>
    public long TotalBytesRead
    {
        get
        {
            return _totalBytesRead;
        }
    }

    /// <summary>
    /// Indicates the current CRC for all blocks slurped in.
    /// </summary>
    public int Crc32Result
    {
        get
        {
            return unchecked((int)(~_register));
        }
    }

    /// <summary>
    /// Returns the CRC32 for the specified stream.
    /// </summary>
    /// <param name="input">The stream over which to calculate the CRC32</param>
    /// <returns>the CRC32 calculation</returns>
    public int GetCrc32(Stream input)
    {
        return GetCrc32AndCopy(input, null);
    }

    /// <summary>
    /// Returns the CRC32 for the specified stream, and writes the input into the
    /// output stream.
    /// </summary>
    /// <param name="input">The stream over which to calculate the CRC32</param>
    /// <param name="output">The stream into which to deflate the input</param>
    /// <returns>the CRC32 calculation</returns>
    public int GetCrc32AndCopy(Stream input, Stream? output)
    {
        if (input == null)
            throw new Exception("The input stream must not be null.");

        unchecked
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            int readSize = BUFFER_SIZE;

            _totalBytesRead = 0;
            int count = input.Read(buffer, 0, readSize);
            if (output != null) output.Write(buffer, 0, count);
            _totalBytesRead += count;
            while (count > 0)
            {
                SlurpBlock(buffer, 0, count);
                count = input.Read(buffer, 0, readSize);
                if (output != null) output.Write(buffer, 0, count);
                _totalBytesRead += count;
            }

            return (int)(~_register);
        }
    }


    /// <summary>
    ///   Get the CRC32 for the given (word,byte) combo.  This is a
    ///   computation defined by PKzip for PKZIP 2.0 (weak) encryption.
    /// </summary>
    /// <param name="w">The word to start with.</param>
    /// <param name="b">The byte to combine it with.</param>
    /// <returns>The CRC-ized result.</returns>
    public int ComputeCrc32(int w, byte b)
    {
        return _InternalComputeCrc32((uint)w, b);
    }

    internal int _InternalComputeCrc32(uint w, byte b)
    {
        return (int)(_crc32Table[(w ^ b) & 0xFF] ^ (w >> 8));
    }


    /// <summary>
    /// Update the value for the running CRC32 using the given block of bytes.
    /// This is useful when using the CRC32() class in a Stream.
    /// </summary>
    /// <param name="block">block of bytes to slurp</param>
    /// <param name="offset">starting point in the block</param>
    /// <param name="count">how many bytes within the block to slurp</param>
    public void SlurpBlock(byte[] block, int offset, int count)
    {
        if (block == null)
            throw new Exception("The data buffer must not be null.");

        // bzip algorithm
        for (int i = 0; i < count; i++)
        {
            int x = offset + i;
            byte b = block[x];
            if (_reverseBits)
            {
                uint temp = (_register >> 24) ^ b;
                _register = (_register << 8) ^ _crc32Table[temp];
            }
            else
            {
                uint temp = (_register & 0x000000FF) ^ b;
                _register = (_register >> 8) ^ _crc32Table[temp];
            }
        }
        _totalBytesRead += count;
    }


    /// <summary>
    ///   Process one byte in the CRC.
    /// </summary>
    /// <param name = "b">the byte to include into the CRC .  </param>
    public void UpdateCRC(byte b)
    {
        if (_reverseBits)
        {
            uint temp = (_register >> 24) ^ b;
            _register = (_register << 8) ^ _crc32Table[temp];
        }
        else
        {
            uint temp = (_register & 0x000000FF) ^ b;
            _register = (_register >> 8) ^ _crc32Table[temp];
        }
    }

    /// <summary>
    ///   Process a run of N identical bytes into the CRC.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This method serves as an optimization for updating the CRC when a
    ///     run of identical bytes is found. Rather than passing in a buffer of
    ///     length n, containing all identical bytes b, this method accepts the
    ///     byte value and the length of the (virtual) buffer - the length of
    ///     the run.
    ///   </para>
    /// </remarks>
    /// <param name = "b">the byte to include into the CRC.  </param>
    /// <param name = "n">the number of times that byte should be repeated. </param>
    public void UpdateCRC(byte b, int n)
    {
        while (n-- > 0)
        {
            if (_reverseBits)
            {
                uint temp = (_register >> 24) ^ b;
                _register = (_register << 8) ^ _crc32Table[(temp >= 0)
                    ? temp
                    : (temp + 256)];
            }
            else
            {
                uint temp = (_register & 0x000000FF) ^ b;
                _register = (_register >> 8) ^ _crc32Table[(temp >= 0)
                    ? temp
                    : (temp + 256)];

            }
        }
    }



    private static uint ReverseBits(uint data)
    {
        unchecked
        {
            uint ret = data;
            ret = (ret & 0x55555555) << 1 | (ret >> 1) & 0x55555555;
            ret = (ret & 0x33333333) << 2 | (ret >> 2) & 0x33333333;
            ret = (ret & 0x0F0F0F0F) << 4 | (ret >> 4) & 0x0F0F0F0F;
            ret = (ret << 24) | ((ret & 0xFF00) << 8) | ((ret >> 8) & 0xFF00) | (ret >> 24);
            return ret;
        }
    }

    private static byte ReverseBits(byte data)
    {
        unchecked
        {
            uint u = (uint)data * 0x00020202;
            uint m = 0x01044010;
            uint s = u & m;
            uint t = (u << 2) & (m << 1);
            return (byte)((0x01001001 * (s + t)) >> 24);
        }
    }



    private void GenerateLookupTable()
    {
        _crc32Table = new uint[256];
        unchecked
        {
            byte i = 0;
            do
            {
                uint dwCrc = i;
                for (byte j = 8; j > 0; j--)
                {
                    if ((dwCrc & 1) == 1)
                    {
                        dwCrc = (dwCrc >> 1) ^ _dwPolynomial;
                    }
                    else
                    {
                        dwCrc >>= 1;
                    }
                }
                if (_reverseBits)
                {
                    _crc32Table[ReverseBits(i)] = ReverseBits(dwCrc);
                }
                else
                {
                    _crc32Table[i] = dwCrc;
                }
                i++;
            } while (i!=0);
        }

#if VERBOSE
            Console.WriteLine();
            Console.WriteLine("private static readonly UInt32[] crc32Table = {");
            for (int i = 0; i < crc32Table.Length; i+=4)
            {
                Console.Write("   ");
                for (int j=0; j < 4; j++)
                {
                    Console.Write(" 0x{0:X8}U,", crc32Table[i+j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("};");
            Console.WriteLine();
#endif
    }


    private uint gf2_matrix_times(uint[] matrix, uint vec)
    {
        uint sum = 0;
        int i=0;
        while (vec != 0)
        {
            if ((vec & 0x01)== 0x01)
                sum ^= matrix[i];
            vec >>= 1;
            i++;
        }
        return sum;
    }

    private void gf2_matrix_square(uint[] square, uint[] mat)
    {
        for (int i = 0; i < 32; i++)
            square[i] = gf2_matrix_times(mat, mat[i]);
    }



    /// <summary>
    ///   Combines the given CRC32 value with the current running total.
    /// </summary>
    /// <remarks>
    ///   This is useful when using a divide-and-conquer approach to
    ///   calculating a CRC.  Multiple threads can each calculate a
    ///   CRC32 on a segment of the data, and then combine the
    ///   individual CRC32 values at the end.
    /// </remarks>
    /// <param name="crc">the crc value to be combined with this one</param>
    /// <param name="length">the length of data the CRC value was calculated on</param>
    public void Combine(int crc, int length)
    {
        uint[] even = new uint[32];     // even-power-of-two zeros operator
        uint[] odd = new uint[32];      // odd-power-of-two zeros operator

        if (length == 0)
            return;

        uint crc1= ~_register;
        uint crc2= (uint) crc;

        // put operator for one zero bit in odd
        odd[0] = _dwPolynomial;  // the CRC-32 polynomial
        uint row = 1;
        for (int i = 1; i < 32; i++)
        {
            odd[i] = row;
            row <<= 1;
        }

        // put operator for two zero bits in even
        gf2_matrix_square(even, odd);

        // put operator for four zero bits in odd
        gf2_matrix_square(odd, even);

        uint len2 = (uint) length;

        // apply len2 zeros to crc1 (first square will put the operator for one
        // zero byte, eight zero bits, in even)
        do {
            // apply zeros operator for this bit of len2
            gf2_matrix_square(even, odd);

            if ((len2 & 1)== 1)
                crc1 = gf2_matrix_times(even, crc1);
            len2 >>= 1;

            if (len2 == 0)
                break;

            // another iteration of the loop with odd and even swapped
            gf2_matrix_square(odd, even);
            if ((len2 & 1)==1)
                crc1 = gf2_matrix_times(odd, crc1);
            len2 >>= 1;


        } while (len2 != 0);

        crc1 ^= crc2;

        _register= ~crc1;

        //return (int) crc1;
    }


    /// <summary>
    ///   Create an instance of the CRC32 class using the default settings: no
    ///   bit reversal, and a polynomial of 0xEDB88320.
    /// </summary>
    public CRC32() : this(false)
    {
    }

    /// <summary>
    ///   Create an instance of the CRC32 class, specifying whether to reverse
    ///   data bits or not.
    /// </summary>
    /// <param name='reverseBits'>
    ///   specify true if the instance should reverse data bits.
    /// </param>
    /// <remarks>
    ///   <para>
    ///     In the CRC-32 used by BZip2, the bits are reversed. Therefore if you
    ///     want a CRC32 with compatibility with BZip2, you should pass true
    ///     here. In the CRC-32 used by GZIP and PKZIP, the bits are not
    ///     reversed; Therefore if you want a CRC32 with compatibility with
    ///     those, you should pass false.
    ///   </para>
    /// </remarks>
    public CRC32(bool reverseBits) :
        this( unchecked((int)0xEDB88320), reverseBits)
    {
    }


    /// <summary>
    ///   Create an instance of the CRC32 class, specifying the polynomial and
    ///   whether to reverse data bits or not.
    /// </summary>
    /// <param name='polynomial'>
    ///   The polynomial to use for the CRC, expressed in the reversed (LSB)
    ///   format: the highest ordered bit in the polynomial value is the
    ///   coefficient of the 0th power; the second-highest order bit is the
    ///   coefficient of the 1 power, and so on. Expressed this way, the
    ///   polynomial for the CRC-32C used in IEEE 802.3, is 0xEDB88320.
    /// </param>
    /// <param name='reverseBits'>
    ///   specify true if the instance should reverse data bits.
    /// </param>
    ///
    /// <remarks>
    ///   <para>
    ///     In the CRC-32 used by BZip2, the bits are reversed. Therefore if you
    ///     want a CRC32 with compatibility with BZip2, you should pass true
    ///     here for the <c>reverseBits</c> parameter. In the CRC-32 used by
    ///     GZIP and PKZIP, the bits are not reversed; Therefore if you want a
    ///     CRC32 with compatibility with those, you should pass false for the
    ///     <c>reverseBits</c> parameter.
    ///   </para>
    /// </remarks>
    public CRC32(int polynomial, bool reverseBits)
    {
        this._reverseBits = reverseBits;
        _dwPolynomial = (uint) polynomial;
        GenerateLookupTable();
    }

    /// <summary>
    ///   Reset the CRC-32 class - clear the CRC "remainder register."
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Use this when employing a single instance of this class to compute
    ///     multiple, distinct CRCs on multiple, distinct data blocks.
    ///   </para>
    /// </remarks>
    public void Reset()
    {
        _register = 0xFFFFFFFFU;
    }

    // private member vars
    private uint _dwPolynomial;
    private long _totalBytesRead;
    private bool _reverseBits;
    private uint[] _crc32Table;
    private const int BUFFER_SIZE = 8192;
    private uint _register = 0xFFFFFFFFU;
}


/// <summary>
/// A Stream that calculates a CRC32 (a checksum) on all bytes read,
/// or on all bytes written.
/// </summary>
///
/// <remarks>
/// <para>
/// This class can be used to verify the CRC of a ZipEntry when
/// reading from a stream, or to calculate a CRC when writing to a
/// stream.  The stream should be used to either read, or write, but
/// not both.  If you intermix reads and writes, the results are not
/// defined.
/// </para>
///
/// <para>
/// This class is intended primarily for use internally by the
/// DotNetZip library.
/// </para>
/// </remarks>
public class CrcCalculatorStream : Stream, IDisposable
{
    private const long UnsetLengthLimit = -99;

    readonly Stream _innerStream;
    readonly CRC32 _crc32;
    readonly long _lengthLimit = -99;
    bool _leaveOpen;

    /// <summary>
    /// The default constructor.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Instances returned from this constructor will leave the underlying
    ///     stream open upon Close().  The stream uses the default CRC32
    ///     algorithm, which implies a polynomial of 0xEDB88320.
    ///   </para>
    /// </remarks>
    /// <param name="stream">The underlying stream</param>
    public CrcCalculatorStream(Stream stream)
        : this(true, UnsetLengthLimit, stream, null)
    {
    }

    /// <summary>
    ///   The constructor allows the caller to specify how to handle the
    ///   underlying stream at close.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The stream uses the default CRC32 algorithm, which implies a
    ///     polynomial of 0xEDB88320.
    ///   </para>
    /// </remarks>
    /// <param name="stream">The underlying stream</param>
    /// <param name="leaveOpen">true to leave the underlying stream
    /// open upon close of the <c>CrcCalculatorStream</c>; false otherwise.</param>
    public CrcCalculatorStream(Stream stream, bool leaveOpen)
        : this(leaveOpen, UnsetLengthLimit, stream, null)
    {
    }

    /// <summary>
    ///   A constructor allowing the specification of the length of the stream
    ///   to read.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The stream uses the default CRC32 algorithm, which implies a
    ///     polynomial of 0xEDB88320.
    ///   </para>
    ///   <para>
    ///     Instances returned from this constructor will leave the underlying
    ///     stream open upon Close().
    ///   </para>
    /// </remarks>
    /// <param name="stream">The underlying stream</param>
    /// <param name="length">The length of the stream to slurp</param>
    public CrcCalculatorStream(Stream stream, long length)
        : this(true, length, stream, null)
    {
        if (length < 0)
            throw new ArgumentException("length");
    }

    /// <summary>
    ///   A constructor allowing the specification of the length of the stream
    ///   to read, as well as whether to keep the underlying stream open upon
    ///   Close().
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The stream uses the default CRC32 algorithm, which implies a
    ///     polynomial of 0xEDB88320.
    ///   </para>
    /// </remarks>
    /// <param name="stream">The underlying stream</param>
    /// <param name="length">The length of the stream to slurp</param>
    /// <param name="leaveOpen">true to leave the underlying stream
    /// open upon close of the <c>CrcCalculatorStream</c>; false otherwise.</param>
    public CrcCalculatorStream(Stream stream, long length, bool leaveOpen)
        : this(leaveOpen, length, stream, null)
    {
        if (length < 0)
            throw new ArgumentException("length");
    }

    /// <summary>
    ///   A constructor allowing the specification of the length of the stream
    ///   to read, as well as whether to keep the underlying stream open upon
    ///   Close(), and the CRC32 instance to use.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The stream uses the specified CRC32 instance, which allows the
    ///     application to specify how the CRC gets calculated.
    ///   </para>
    /// </remarks>
    /// <param name="stream">The underlying stream</param>
    /// <param name="length">The length of the stream to slurp</param>
    /// <param name="leaveOpen">true to leave the underlying stream
    /// open upon close of the <c>CrcCalculatorStream</c>; false otherwise.</param>
    /// <param name="crc32">the CRC32 instance to use to calculate the CRC32</param>
    public CrcCalculatorStream(Stream stream, long length, bool leaveOpen,
        CRC32 crc32)
        : this(leaveOpen, length, stream, crc32)
    {
        if (length < 0)
            throw new ArgumentException("length");
    }


    // This ctor is private - no validation except null is done here.
    // This is to allow the use
    // of a (specific) negative value for the _lengthLimit, to indicate that there
    // is no length set.  So we validate the length limit in those ctors that use an
    // explicit param, otherwise we don't validate, because it could be our special
    // value.
    CrcCalculatorStream(bool leaveOpen, long length, Stream stream, CRC32 crc32)
    {
        if (stream == null) throw new ArgumentNullException("stream");
        _innerStream = stream;
        _crc32 = crc32 ?? new CRC32();
        _lengthLimit = length;
        _leaveOpen = leaveOpen;
    }


    /// <summary>
    ///   Gets the total number of bytes run through the CRC32 calculator.
    /// </summary>
    ///
    /// <remarks>
    ///   This is either the total number of bytes read, or the total number of
    ///   bytes written, depending on the direction of this stream.
    /// </remarks>
    public long TotalBytesSlurped
    {
        get { return _crc32.TotalBytesRead; }
    }

    /// <summary>
    ///   Provides the current CRC for all blocks slurped in.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The running total of the CRC is kept as data is written or read
    ///     through the stream.  read this property after all reads or writes to
    ///     get an accurate CRC for the entire stream.
    ///   </para>
    /// </remarks>
    public int Crc
    {
        get { return _crc32.Crc32Result; }
    }

    /// <summary>
    ///   Indicates whether the underlying stream will be left open when the
    ///   <c>CrcCalculatorStream</c> is Closed.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Set this at any point before calling <see cref="Close()"/>.
    ///   </para>
    /// </remarks>
    public bool LeaveOpen
    {
        get { return _leaveOpen; }
        set { _leaveOpen = value; }
    }

    /// <summary>
    /// Read from the stream
    /// </summary>
    /// <param name="buffer">the buffer to read</param>
    /// <param name="offset">the offset at which to start</param>
    /// <param name="count">the number of bytes to read</param>
    /// <returns>the number of bytes actually read</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesToRead = count;

        // Need to limit the # of bytes returned, if the stream is intended to have
        // a definite length.  This is especially useful when returning a stream for
        // the uncompressed data directly to the application.  The app won't
        // necessarily read only the UncompressedSize number of bytes.  For example
        // wrapping the stream returned from OpenReader() into a StreadReader() and
        // calling ReadToEnd() on it, We can "over-read" the zip data and get a
        // corrupt string.  The length limits that, prevents that problem.

        if (_lengthLimit != UnsetLengthLimit)
        {
            if (_crc32.TotalBytesRead >= _lengthLimit) return 0; // EOF
            long bytesRemaining = _lengthLimit - _crc32.TotalBytesRead;
            if (bytesRemaining < count) bytesToRead = (int)bytesRemaining;
        }
        int n = _innerStream.Read(buffer, offset, bytesToRead);
        if (n > 0) _crc32.SlurpBlock(buffer, offset, n);
        return n;
    }

    /// <summary>
    /// Write to the stream.
    /// </summary>
    /// <param name="buffer">the buffer from which to write</param>
    /// <param name="offset">the offset at which to start writing</param>
    /// <param name="count">the number of bytes to write</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (count > 0) _crc32.SlurpBlock(buffer, offset, count);
        _innerStream.Write(buffer, offset, count);
    }

    /// <summary>
    /// Indicates whether the stream supports reading.
    /// </summary>
    public override bool CanRead
    {
        get { return _innerStream.CanRead; }
    }

    /// <summary>
    ///   Indicates whether the stream supports seeking.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Always returns false.
    ///   </para>
    /// </remarks>
    public override bool CanSeek
    {
        get { return false; }
    }

    /// <summary>
    /// Indicates whether the stream supports writing.
    /// </summary>
    public override bool CanWrite
    {
        get { return _innerStream.CanWrite; }
    }

    /// <summary>
    /// Flush the stream.
    /// </summary>
    public override void Flush()
    {
        _innerStream.Flush();
    }

    /// <summary>
    ///   Returns the length of the underlying stream.
    /// </summary>
    public override long Length
    {
        get
        {
            if (_lengthLimit == UnsetLengthLimit)
                return _innerStream.Length;
            return _lengthLimit;
        }
    }

    /// <summary>
    ///   The getter for this property returns the total bytes read.
    ///   If you use the setter, it will throw
    /// <see cref="NotSupportedException"/>.
    /// </summary>
    public override long Position
    {
        get { return _crc32.TotalBytesRead; }
        set { throw new NotSupportedException(); }
    }

    /// <summary>
    /// Seeking is not supported on this stream. This method always throws
    /// <see cref="NotSupportedException"/>
    /// </summary>
    /// <param name="offset">N/A</param>
    /// <param name="origin">N/A</param>
    /// <returns>N/A</returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// This method always throws
    /// <see cref="NotSupportedException"/>
    /// </summary>
    /// <param name="value">N/A</param>
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }


    void IDisposable.Dispose()
    {
        Close();
    }

    /// <summary>
    /// Closes the stream.
    /// </summary>
    public override void Close()
    {
        base.Close();
        if (!_leaveOpen)
            _innerStream.Close();
    }

}