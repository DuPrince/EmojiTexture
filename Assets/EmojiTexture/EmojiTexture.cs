﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class EmojiTexture
{

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void EmojiTexture_render(string text, IntPtr buffer, int width, int height);
#elif UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaClass _EmojiTextureClass;
    private static AndroidJavaClass EmojiTextureClass {
        get {
            if(_EmojiTextureClass == null){
                _EmojiTextureClass = new AndroidJavaClass("com.ibicha.emojitexture.EmojiTexture");
            }
            return _EmojiTextureClass;
        }
    }
#endif

    /// <summary>
    /// Get or set how the text if filtered. If set to true, the first emoji character found is extracted from the text to be rendered.
    /// If set to false, text is rendered as is. Defaults to true.
    /// </summary>
    public bool SanitizeText
    {
        get
        {
            return sanitizeText;
        }
        set
        {
            sanitizeText = value;
            this.Text = Text;
        }
    }

    /// <summary>
    /// Get or set the text of the emoji. Usually a one character string representing the emoji.
    /// </summary>
    /// <value>Emoji string</value>
    public string Text
    {
        get
        {
            return text;
        }
        set
        {
            text = SanitizeText ? GetEmojiString(value) : value;
            Render();
        }
    }

    /// <summary>
    /// Copies the pixels from the native buffer and returns a byte array in the RGBA32 format.
    /// </summary>
    /// <value>The byte buffer.</value>
    public byte[] ByteBuffer
    {
        get
        {
            if (isByteBufferDirty)
            {
                if (buffer != IntPtr.Zero && bufferSize > 0)
                {
                    if (byteBuffer == null)
                        byteBuffer = new byte[bufferSize];
                    Marshal.Copy(buffer, byteBuffer, 0, bufferSize);
                    isByteBufferDirty = false;
                }
            }
            return byteBuffer;
        }
    }

    private void Render()
    {
        //Rendering
#if UNITY_IOS && !UNITY_EDITOR
        EmojiTexture_render(text, buffer, texture.width, texture.height);
#elif UNITY_ANDROID && !UNITY_EDITOR
        EmojiTextureClass.CallStatic("render", text, jByteBuffer, texture.width, texture.height);
#endif
        //Copy pixels to texture
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        isByteBufferDirty = true;
        texture.LoadRawTextureData(buffer, bufferSize);
        texture.Apply();
#endif

    }

    private Texture2D texture;
    private string text;
    private bool sanitizeText;
    private IntPtr buffer;
    private AndroidJavaObject jByteBuffer;
    private int bufferSize;

    private byte[] byteBuffer;
    private bool isByteBufferDirty;

    public EmojiTexture() : this(null) { }

    public EmojiTexture(string text) : this(text, 256, 256) { }

    public EmojiTexture(string text, int size) : this(text, size, size) { }

    public EmojiTexture(int size) : this(null, size, size) { }

    public EmojiTexture(int width, int height) : this(null, width, height) { }

    public EmojiTexture(string text, int width, int height)
    {
        width = Mathf.Clamp(width, 8, 256);
        height = Mathf.Clamp(height, 8, 256);
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        bufferSize = 0;
        buffer = IntPtr.Zero;
        byteBuffer = null;
        isByteBufferDirty = false;
        sanitizeText = true;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        bufferSize = width * height * 4;
        buffer = Marshal.AllocHGlobal(bufferSize);
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        jByteBuffer = new AndroidJavaObject("java.nio.DirectByteBuffer", buffer.ToInt64(), bufferSize);
#endif

        Text = text;
    }

    ~EmojiTexture()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        if(buffer != IntPtr.Zero)
            Marshal.FreeHGlobal(buffer);
#endif
    }

    static public implicit operator Texture2D(EmojiTexture emojiTexture)
    {
        return emojiTexture == null ? null : emojiTexture.texture;
    }

    /// <summary>
    /// Extracts the first emoji character from the text. returns String.Empty if no emojis are found.
    /// </summary>
    /// <param name="text">The input text</param>
    /// <returns>A string representing an emoji. String.Empty if not emojis found in the input text.</returns>
    public static string GetEmojiString(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"(?:[\u2700-\u27bf]|(?:\ud83c[\udde6-\uddff]){2}|[\ud800-\udbff][\udc00-\udfff]|[\u0023-\u0039]\ufe0f?\u20e3|\u3299|\u3297|\u303d|\u3030|\u24c2|\ud83c[\udd70-\udd71]|\ud83c[\udd7e-\udd7f]|\ud83c\udd8e|\ud83c[\udd91-\udd9a]|\ud83c[\udde6-\uddff]|[\ud83c[\ude01-\ude02]|\ud83c\ude1a|\ud83c\ude2f|[\ud83c[\ude32-\ude3a]|[\ud83c[\ude50-\ude51]|\u203c|\u2049|[\u25aa-\u25ab]|\u25b6|\u25c0|[\u25fb-\u25fe]|\u00a9|\u00ae|\u2122|\u2139|\ud83c\udc04|[\u2600-\u26FF]|\u2b05|\u2b06|\u2b07|\u2b1b|\u2b1c|\u2b50|\u2b55|\u231a|\u231b|\u2328|\u23cf|[\u23e9-\u23f3]|[\u23f8-\u23fa]|\ud83c\udccf|\u2934|\u2935|[\u2190-\u21ff])");
            if (match.Success)
            {
                return match.Value;
            }
        }
        return String.Empty;
    }
}