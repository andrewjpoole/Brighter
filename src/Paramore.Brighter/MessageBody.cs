﻿#region Licence
/* The MIT License (MIT)
Copyright © 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Serialization;
using Paramore.Brighter.Extensions;

namespace Paramore.Brighter
{
    /// <summary>
    /// Class MessageBody
    /// The body of a <see cref="Message"/>
    /// </summary>
    public class MessageBody : IEquatable<MessageBody>
    {
        /// <summary>
        /// The message body as a byte array.
        /// </summary>
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// The type of message encoded into Bytes.  A hint for deserialization that 
        /// will be sent with the byte[] to allow
        /// </summary>
        public ContentType? ContentType { get; set; }

        /// <summary>
        /// What is  the character encoding of the text in the message
        /// </summary>
        public CharacterEncoding CharacterEncoding { get; private set; }

        /// <summary>
        /// The message body as a string.
        /// If the message body is UTF8 or ASCII, this will be the same as the string representation of the byte array.
        /// If the message body is Base64 encoded, this will be the encoded string.
        /// if the message body is compressed, it will throw an exception, you should decompress the bytes first. 
        /// </summary>
        /// <value>The value.</value>
        public string Value
        {
            get
            {
                switch (CharacterEncoding)
                {
                    case CharacterEncoding.Base64:
                    case CharacterEncoding.Raw:
                        return Convert.ToBase64String(Bytes);
                    case CharacterEncoding.UTF8:
                        return Encoding.UTF8.GetString(Bytes);
                    case CharacterEncoding.ASCII:
                        return Encoding.ASCII.GetString(Bytes);
                    default:
                        throw new InvalidCastException(
                            $"Message Body with {CharacterEncoding} is not available");
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBody"/> class with a string.  Use Value property to retrieve.
        /// </summary>
        /// <param name="body">The body of the message, usually XML or JSON.</param>
        /// <param name="contentType">The type of the message, usually "application/json". Defaults to "application/json"</param>
        /// <param name="characterEncoding">The encoding of the content. Defaults to MessageEncoding.UTF8.
        /// If you pass us "application/octet" but the type is ascii or utf8, we will convert to base64 for you.
        /// </param>
        public MessageBody(string? body, ContentType? contentType = null, CharacterEncoding characterEncoding = CharacterEncoding.UTF8)
        {
#if NETSTANDARD2_0
            ContentType = contentType ?? new ContentType("application/json");
            SetCharacterEncoding(ContentType, characterEncoding);
#else            
            ContentType = contentType ?? new ContentType(MediaTypeNames.Application.Json);
            SetCharacterEncoding(ContentType, characterEncoding);
#endif
            CharacterEncoding = characterEncoding;
            
            if (characterEncoding == CharacterEncoding.Raw) throw new ArgumentOutOfRangeException("characterEncoding", "Raw encoding is not supported for string constructor");

            if (body == null)
            {
                Bytes = Array.Empty<byte>();
                return;
            }
            
            Bytes = (CharacterEncoding switch
            {
                CharacterEncoding.Base64 => Convert.FromBase64String(body),
                CharacterEncoding.UTF8 => Encoding.UTF8.GetBytes(body),
                CharacterEncoding.ASCII => Encoding.ASCII.GetBytes(body),
                _ => Bytes
            })!;
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBody"/> class using a byte array.
        /// </summary>
        /// <param name="bytes">The Body of the Message</param>
        /// <param name="contentType">The content type of message encoded in body</param>
        /// <param name="characterEncoding"></param>
        [JsonConstructor]
        public MessageBody(byte[]? bytes, ContentType? contentType = null,  CharacterEncoding characterEncoding = CharacterEncoding.UTF8)
        {
#if NETSTANDARD2_0
            ContentType = contentType ?? new ContentType("application/json");
            SetCharacterEncoding(ContentType, characterEncoding);
#else            
            ContentType = contentType ?? new ContentType(MediaTypeNames.Application.Json);
            SetCharacterEncoding(ContentType, characterEncoding);
#endif
            CharacterEncoding = characterEncoding;
            
            if (bytes is null)
            {
                Bytes = [];
                return;
            }
            
            Bytes = bytes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBody"/> class using a byte array.
        /// TODO: We don't support the range of options on Span on netstandard2.0 that let's us  flow through a ReadOnlyMemory
        /// for serialization so we allocate here as well as in PullConsumer when we probably don't need this allocation.
        /// </summary>
        /// <param name="body">The body of the message in bytes</param>
        /// <param name="contentType">The content type of the body</param>
        /// <param name="characterEncoding">The encoding of any text in the body</param>
        public MessageBody(in ReadOnlyMemory<byte> body, ContentType? contentType = null, CharacterEncoding characterEncoding = CharacterEncoding.UTF8)
        {
#if NETSTANDARD2_0
            ContentType = contentType ?? new ContentType("application/json");
            SetCharacterEncoding(ContentType, characterEncoding);
#else            
            ContentType = contentType ?? new ContentType(MediaTypeNames.Application.Json);
            SetCharacterEncoding(ContentType, characterEncoding);
#endif
            Bytes = body.ToArray();
            CharacterEncoding = characterEncoding;
        }


        /// <summary>
        /// Converts the body to a character-encoded string.
        /// </summary>
        /// <returns></returns>
        public string ToCharacterEncodedString(CharacterEncoding characterEncoding)
        {
            return characterEncoding switch
            {
                CharacterEncoding.Base64 => Convert.ToBase64String(Bytes),
                CharacterEncoding.UTF8 => Encoding.UTF8.GetString(Bytes),
                CharacterEncoding.ASCII => Encoding.ASCII.GetString(Bytes),
                _ => throw new InvalidOperationException($"Message Body with {CharacterEncoding} is not available")
            };
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(MessageBody? other)
        {
            if (other is null) return false;
            var bodyEqual = Bytes.SequenceEqual(other.Bytes);
            var sameContentType = ContentType is null || ContentType.Equals(other.ContentType); 
            return bodyEqual && sameContentType ;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MessageBody)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return (Bytes is not null ? Bytes.GetHashCode() : 0);
        }

        /// <summary>
        /// Implements the ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(MessageBody? left, MessageBody? right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(MessageBody? left, MessageBody? right)
        {
            return !Equals(left, right);
        }
        
        private void SetCharacterEncoding(ContentType? contentType, CharacterEncoding characterEncoding)
        {
            var characterEncodingString = characterEncoding.FromCharacterEncoding();
            if (contentType is not null && characterEncodingString is not null)
                contentType.CharSet = characterEncodingString;
        }
    }
}
