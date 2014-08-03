//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;

namespace BluetoothRfcommUniversalApp
{
    // BluetoothCommand class methods Return code
    public enum BluetoothCommandReturnCode
    {
        Success = 0,
        Error,
        UndefinedCommand,
        FormatError,
        BufferEmpty,

    }
    // BluetoothCommand class 
    // 
    // This class implements the communication between the Windows Phone Application 
    // and the Windows Application. Both applications can exchanges BluetoothCommands.
    // For this sample application, only Message Commands and Picture Commands are implemented.
    // The Message Command transport message text and the Picture Command transport Picture files.
    // 
    // The syntax of the exchanges is the following:
    //    command=<BluetoothCommand>|[[<Attribute0Key>=<Attribute0>]|[<Attribute1Key>=<Attribute1>] ... |[<AttributeNKey>=<AttributeN>]...]]
    // For instance:
    // Message Command:
    //    command=message|content=<Message Content>
    // Picture File Command:
    //    command=picture|path=<Picture File Path>|size=<Picture File Size>|content=<Picture File Content>
    //
    // The subclasses BluetoothCommandMessage and BluetoothCommandPicture implement the communi
    public class BluetoothCommand
    {
        // Command Key
        public const string BluetoothCommandKey = "command";
        // Message Command Key
        public const string BluetoothCommandMessage = "message";
        // Picture Command Key
        public const string BluetoothCommandPicture = "picture";

        // Command Separator
        public const char BluetoothCommandSeparator = '|';
        // Command Equal
        public const char BluetoothCommandEqual = '=';

        // Buffer sent over Bluetooth
        public Byte[] CommandBytes { get { return _CommandBytes; } }
        // Buffer Length
        public UInt32 CommandLength { get { return (UInt32)_CommandBytes.Length; } }
        protected Byte[] _CommandBytes;
        
        // BluetoothCommand Constructor
        public BluetoothCommand()
        {
            _CommandBytes = null;
        }
        // CreateBluetoothCommmand method create a BluetoothCommand with the bytes received over bluetooth
        // 
        // This method parses the bytes to create either a BluetoothCommandMessage or a BluetoothCommandPicture.
        public static BluetoothCommand CreateBluetoothCommmand(byte[] buffer)
        {
            if(buffer!=null)
            {
                int i = GetIndexNextSeparator(buffer, 0);
                if ((0<i) && (i < buffer.Length))
                {
                    string s = System.Text.Encoding.UTF8.GetString(buffer, 0,(int) i);
                    char[] sep = new char[] { BluetoothCommandEqual };
                    String[] ArrayString = s.Split(sep);
                    if((ArrayString!=null)&&(ArrayString.Length==2))
                    {
                        BluetoothCommand bg = null;
                        if (ArrayString[1] == BluetoothCommandMessage)
                        {
                            bg = new BluetoothCommandMessage();
                        } else if (ArrayString[1] == BluetoothCommandPicture)
                        {
                            bg = new BluetoothCommandPicture();
                        }
                        if(bg!=null)
                        {
                            BluetoothCommandReturnCode r = bg.GetAttributes(buffer,i);
                            if (r == BluetoothCommandReturnCode.Success)
                                return bg;
                        }
                    }
                }
            }
            return null;
        }
        // virtual GetAttributes method
        // 
        public virtual BluetoothCommandReturnCode GetAttributes(byte[] buffer, int Index)
        {
            return BluetoothCommandReturnCode.Success;
        }
        public string GetCommandString()
        {
            string returnString = "Unknown command";
            if (_CommandBytes!= null)
            {
                int i = GetIndexNextSeparator(_CommandBytes, 0);
                if ((0 < i) && (i < _CommandBytes.Length))
                {
                    string s = System.Text.Encoding.UTF8.GetString(_CommandBytes, 0, (int)i);
                    char[] sep = new char[] { BluetoothCommandEqual };
                    String[] ArrayString = s.Split(sep);
                    if ((ArrayString != null) && (ArrayString.Length == 2))
                    {
                        if (ArrayString[1] == BluetoothCommandMessage)
                        {
                            returnString = "Message Command";
                        }
                        else if (ArrayString[1] == BluetoothCommandPicture)
                        {
                            returnString = "Picture Command";
                        }
                    }
                }
            }
            return returnString;
        }

        // ToString virtual method
        public virtual string ToString()
        {
            return GetCommandString();
        }
        // GetIndexNextSeparator method
        // return the index in the buffer of the next separator
        public static int GetIndexNextSeparator(byte[] buffer, int Index)
        {
            for (int i = Index; i < buffer.Length; i++)
                if (buffer[i] == BluetoothCommandSeparator)
                    return i;
            return -1;
        }
    }
    // BluetoothCommandMessage  class
    // 
    // Implements BluetoothCommandMessage 
    public class BluetoothCommandMessage : BluetoothCommand
    {
        // Content Attribute Key
        public const string BluetoothContentAttributeKey = "content";
        // Message Content 
        public string MessageContent;

        // constructor
        public BluetoothCommandMessage()
        {
            MessageContent = string.Empty;
        }
        // Initialize the Message Attributes (MessageContent)
        // after parsing the received bytes.
        public override BluetoothCommandReturnCode GetAttributes(byte[] buffer, int Index)
        {
            int i = Index + 1;
            while (i < buffer.Length)
            {
                for (; i < buffer.Length; i++)
                {
                    if (buffer[i] == BluetoothCommandEqual)
                        break;
                }
                if (i < buffer.Length)
                {
                    string s = System.Text.Encoding.UTF8.GetString(buffer, Index + 1, i - Index - 1);
                    if (s == BluetoothContentAttributeKey)
                    {
                        if (i + 1 < buffer.Length)
                        {
                            MessageContent = System.Text.Encoding.UTF8.GetString(buffer, i + 1, buffer.Length - i - 1); 
                            return BluetoothCommandReturnCode.Success;
                        }
                        return BluetoothCommandReturnCode.FormatError;
                    }
                    else
                        return BluetoothCommandReturnCode.FormatError;
                }
            }
            return BluetoothCommandReturnCode.FormatError;
        }
        // Fill the CommandBytes based on the Message Text which will be sent to the peer.
        // This method creates the BluetoothCommandMessage
        public BluetoothCommandReturnCode CreateMessageCommand(string Msg)
        {
            MessageContent = Msg;
            string command = BluetoothCommandKey + BluetoothCommandEqual + BluetoothCommandMessage + BluetoothCommandSeparator + BluetoothContentAttributeKey + BluetoothCommandEqual + Msg;
            _CommandBytes = System.Text.Encoding.UTF8.GetBytes(command);
            return BluetoothCommandReturnCode.Success;
        }
        // ToString virtual method
        public override string ToString()
        {
            return "Message: " + MessageContent;
        }
    }
    // BluetoothCommandPicture  class
    // 
    // Implements BluetoothCommandPicture
    public class BluetoothCommandPicture : BluetoothCommand
    {
        // content attribute key
        public const string BluetoothContentAttributeKey = "content";
        // path attribute key
        public const string BluetoothPicturePathAttributeKey = "path";
        // size attribute key
        public const string BluetoothPictureSizeAttributeKey = "size";
        // Picture content
        public byte[] PictureContent;
        // Picture Path
        public string PicturePath;
        // Picture Size
        public int PictureSize;
        // Constructor
        public BluetoothCommandPicture()
        {
            PictureContent = null;
            PicturePath = string.Empty;
            PictureSize = 0;
        }
        // Initialize the Picture Attributes (PictureContent, PicturePath, PictureSize)
        // after parsing the received bytes.
        public override BluetoothCommandReturnCode GetAttributes(byte[] buffer, int Index)
        {
            int i = Index + 1 ;
            while (i < buffer.Length)
            {
                for (; i < buffer.Length; i++)
                {
                    if (buffer[i] == BluetoothCommandEqual)
                        break;
                }
                if (i < buffer.Length)
                {
                    string s = System.Text.Encoding.UTF8.GetString(buffer, Index + 1, i - Index - 1);
                    if (s == BluetoothContentAttributeKey)
                    {
                        if (i + 1 < buffer.Length)
                        {
                            PictureContent = new byte[buffer.Length - i - 1];
                            if (PictureContent != null)
                            {
                                // Ensure the expected picture size is equal with the buffer size
                                if (PictureSize == PictureContent.Length)
                                {
                                    Array.Copy(buffer, i + 1, PictureContent, 0, buffer.Length - i - 1);
                                    return BluetoothCommandReturnCode.Success;
                                }
                            }
                        }
                        return BluetoothCommandReturnCode.FormatError;
                    }
                    else if (s == BluetoothPicturePathAttributeKey)
                    {
                        if (i + 1 < buffer.Length)
                        {
                            Index = GetIndexNextSeparator(buffer, i + 1);
                            PicturePath = System.Text.Encoding.UTF8.GetString(buffer, i + 1, Index - i - 1);
                            i = Index + 1;
                            continue;
                        }
                        return BluetoothCommandReturnCode.FormatError;
                    }
                    else if (s == BluetoothPictureSizeAttributeKey)
                    {
                        if (i + 1 < buffer.Length)
                        {
                            Index = GetIndexNextSeparator(buffer, i + 1);
                            string stringsize = System.Text.Encoding.UTF8.GetString(buffer, i + 1, Index - i - 1);
                            i = Index + 1;
                            int l = 0;
                            if (int.TryParse(stringsize, out l))
                                PictureSize = l;
                            continue;
                        }
                        return BluetoothCommandReturnCode.FormatError;
                    }
                    else
                        return BluetoothCommandReturnCode.FormatError;
                }
            }
            return BluetoothCommandReturnCode.FormatError;
        }
        // Fill the CommandBytes based on the Picture file path, picture file size, picture content which will be sent to the peer.
        // This method creates the BluetoothCommandPicture
        public BluetoothCommandReturnCode CreatePictureCommand(string OriginalFilePath, int FileSize, byte[] FileBuffer)
        {
            PicturePath = OriginalFilePath;
            PictureSize = FileSize;
            if (FileBuffer == null)
                return BluetoothCommandReturnCode.BufferEmpty;
            string command = BluetoothCommandKey + BluetoothCommandEqual + BluetoothCommandPicture
                + BluetoothCommandSeparator + BluetoothPicturePathAttributeKey + BluetoothCommandEqual + PicturePath
                + BluetoothCommandSeparator + BluetoothPictureSizeAttributeKey + BluetoothCommandEqual + PictureSize.ToString()
                + BluetoothCommandSeparator + BluetoothContentAttributeKey + BluetoothCommandEqual;
            byte[] LocalCommandBytes = System.Text.Encoding.UTF8.GetBytes(command);

            _CommandBytes = new byte[LocalCommandBytes.Length + FileBuffer.Length];
            if (_CommandBytes != null)
            {
                LocalCommandBytes.CopyTo(_CommandBytes, 0);
                FileBuffer.CopyTo(_CommandBytes, LocalCommandBytes.Length);

                return BluetoothCommandReturnCode.Success;
            }
            return BluetoothCommandReturnCode.Error;
        }
        // ToString virtual method
        public override string ToString()
        {
            return "Picture Path: " + PicturePath + " Size: " + PictureSize.ToString();
        }
    }
}
