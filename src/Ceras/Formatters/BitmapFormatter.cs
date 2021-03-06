﻿using System;

namespace Ceras.Formatters
{
#if NETFRAMEWORK
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.IO;

	class BitmapFormatter : IFormatter<Bitmap>
	{
		[ThreadStatic]
		static MemoryStream _sharedMemoryStream;

		CerasSerializer _ceras;

		BitmapMode BitmapMode => _ceras.Config.Advanced.BitmapMode;

		public BitmapFormatter()
		{
			CerasSerializer.AddFormatterConstructedType(typeof(Bitmap));
		}

		public void Serialize(ref byte[] buffer, ref int offset, Bitmap img)
		{
			// Let the image serialize itself to the memory stream
			// Its unfortunate that there's only a stream-based api...
			// The alternative would be manually locking the bits.
			// That would be easy, but we'd potentially lose some information (animation frames etc?)

			var mode = BitmapMode;
			var format = BitmapModeToImgFormat(mode);

			if (_sharedMemoryStream == null)
				_sharedMemoryStream = new MemoryStream((int)(4 * (img.Width * img.Height) * 1.35));
			var ms = _sharedMemoryStream;

			ms.Position = 0;
			img.Save(ms, format);

			long sizeLong = ms.Position;
			if (sizeLong > int.MaxValue)
				throw new InvalidOperationException("image too large");
			int size = (int)sizeLong;

			ms.Position = 0;
			var memoryStreamBuffer = ms.GetBuffer();

			// Write Size
			SerializerBinary.WriteUInt32Fixed(ref buffer, ref offset, (uint)size);

			// Write data into serialization buffer
			if (size > 0)
			{
				SerializerBinary.EnsureCapacity(ref buffer, offset, size);
				SerializerBinary.FastCopy(memoryStreamBuffer, 0, buffer, offset, size);
			}

			offset += size;
		}

		public void Deserialize(byte[] buffer, ref int offset, ref Bitmap img)
		{
			// Read data size
			int size = (int)SerializerBinary.ReadUInt32Fixed(buffer, ref offset);

			// Copy data into stream
			if (_sharedMemoryStream == null)
				_sharedMemoryStream = new MemoryStream(size);
			else if (_sharedMemoryStream.Capacity < size)
				_sharedMemoryStream.Capacity = size;

			var ms = _sharedMemoryStream;

			ms.Position = 0;
			var memoryStreamBuffer = ms.GetBuffer();
			
			if (size > 0)
			{
				SerializerBinary.FastCopy(buffer, offset, memoryStreamBuffer, 0, size);
			}

			// Now we can load the image back from the stream
			ms.Position = 0;

			img = new Bitmap(ms);

			offset += size;
		}

		static ImageFormat BitmapModeToImgFormat(BitmapMode mode)
		{
			if (mode == BitmapMode.DontSerializeBitmaps)
				throw new InvalidOperationException("You need to set 'config.Advanced.BitmapMode' to any setting other than 'DontSerializeBitmaps'. Otherwise you need to skip data-members on your classes/structs that contain Image/Bitmap, or serialize them yourself using your own IFormatter<> implementation.");

			if (mode == BitmapMode.SaveAsBmp)
				return ImageFormat.Bmp;
			else if (mode == BitmapMode.SaveAsJpg)
				return ImageFormat.Jpeg;
			else if (mode == BitmapMode.SaveAsPng)
				return ImageFormat.Png;

			throw new ArgumentOutOfRangeException();
		}
	}

	class ColorFormatter : IFormatter<Color>
	{
		public void Serialize(ref byte[] buffer, ref int offset, Color value)
		{
			SerializerBinary.WriteInt32Fixed(ref buffer, ref offset, value.ToArgb());
		}

		public void Deserialize(byte[] buffer, ref int offset, ref Color value)
		{
			var argb = SerializerBinary.ReadInt32Fixed(buffer, ref offset);
			value = Color.FromArgb(argb);
		}
	}

#endif
}
