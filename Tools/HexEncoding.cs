﻿/*  MapleLib - A general-purpose MapleStory library
 * Copyright (C) 2009, 2010 Snow and haha01haha01

 * This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
    along with this. If not, see <http://www.gnu.org/licenses/>.*/

using System;
using System.Linq;

namespace WoongConnector.Tools
{
	/// <summary>
	/// Class to handle Hex Encoding and Hex Conversions
	/// </summary>
	public class HexEncoding
	{

		/// <summary>
		/// Checks if a character is a hex digit
		/// </summary>
		/// <param name="c">Char to check</param>
		/// <returns>Char is a hex digit</returns>
		public static bool IsHexDigit(char c)
		{
			var asciiA = Convert.ToInt32('A');
			var ascii0 = Convert.ToInt32('0');
			c = char.ToUpper(c);
			var numChar = Convert.ToInt32(c);

			if (numChar >= asciiA && numChar < (asciiA + 6))
				return true;

			return numChar >= ascii0 && numChar < (ascii0 + 10);
		}

		/// <summary>
		/// Convert a hex string to a byte
		/// </summary>
		/// <param name="hex">Byte as a hex string</param>
		/// <returns>Byte representation of the string</returns>
		private static byte HexToByte(string hex)
		{
			if (hex.Length > 2 || hex.Length <= 0)
				throw new ArgumentException("Hex must be 1 or 2 characters in length");

			var newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);

			return newByte;
		}

		/// <summary>
		/// Convert a hex string to a byte array
		/// </summary>
		/// <param name="hex">byte array as a hex string</param>
		/// <returns>Byte array representation of the string</returns>
		public static byte[] GetBytes(string hexString)
		{
			// Remove all none A-F, 0-9, characters
			var newString = hexString.Where(IsHexDigit).Aggregate(string.Empty, (current, c) => current + c);

			// If odd number of characters, discard last character
			if (newString.Length % 2 != 0)
			{
				newString = newString.Substring(0, newString.Length - 1);
			}

			var byteLength = newString.Length / 2;
			var bytes = new byte[byteLength];
			var j = 0;

			for (var i = 0; i < bytes.Length; i++)
			{
				var hex = new string(new char[] { newString[j], newString[j + 1] });
				bytes[i] = HexToByte(hex);
				j += 2;
			}

			return bytes;
		}

		/// <summary>
		/// Convert byte array to ASCII
		/// </summary>
		/// <param name="bytes">Bytes to convert to ASCII</param>
		/// <returns>The byte array as an ASCII string</returns>
		public static string ToStringFromAscii(byte[] bytes)
		{
			var ret = new char[bytes.Length];

			for (var x = 0; x < bytes.Length; x++)
			{
				if (bytes[x] < 32 && bytes[x] >= 0)
				{
					ret[x] = '.';
				}

				else
				{
					var chr = ((short)bytes[x]) & 0xFF;
					ret[x] = (char)chr;
				}
			}
			return new string(ret);
		}
	}
}
