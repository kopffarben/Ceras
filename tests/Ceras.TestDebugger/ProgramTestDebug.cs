﻿namespace Ceras.TestDebugger
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing;
	using System.IO;
	using Test;

	class Program
	{
		static void Main(string[] args)
		{
#if NET45
			global::System.Console.WriteLine("Running on NET4.5");
#elif NET451
			global::System.Console.WriteLine("Running on NET4.5.1");
#elif NET452
			global::System.Console.WriteLine("Running on NET4.5.2");
#elif NET47
			global::System.Console.WriteLine("Running on NET4.7");
#elif NET47
			global::System.Console.WriteLine("Running on NET4.7");
#elif NET471
			global::System.Console.WriteLine("Running on NET4.7.1");
#elif NET472
			global::System.Console.WriteLine("Running on NET4.7.2");
#elif NETSTANDARD2_0
			global::System.Console.WriteLine("Running on NET STANDARD 2.0");
#else
#error Unhandled framework version!
#endif





			new Internals().FastCopy();

			new BuiltInTypes().Bitmap();

			var config = new SerializerConfig();
			config.Advanced.BitmapMode = BitmapMode.SaveAsBmp;
			var ceras = new CerasSerializer(config);

			var home = System.Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
			var downloads = Path.Combine(home, "Downloads");

			var images = new Image[]
			{
				Image.FromFile(Path.Combine(downloads, @"68747470733a2f2f692e696d6775722e636f6d2f513839365567562e706e67.png")),
				Image.FromFile(Path.Combine(downloads, @"7plX.gif")),
				Image.FromFile(Path.Combine(downloads, @"TexturesCom_BrickOldMixedSize0012_1_seamless_S.jpg")),
				Image.FromFile(Path.Combine(downloads, @"New Drawing.png")),
				Image.FromFile(Path.Combine(downloads, @"smoke_1_40_128_corrected.png")),
				Image.FromFile(Path.Combine(downloads, @"Spheres_thumb9.png")),
			};

			for (int iteration = 0; iteration < 5; iteration++)
			{
				var imgData1 = ceras.Serialize(images);
				var clones = ceras.Deserialize<Image[]>(imgData1);

				for (var cloneIndex = 0; cloneIndex < clones.Length; cloneIndex++)
				{
					var c = clones[cloneIndex];
					c.Dispose();
					clones[cloneIndex] = null;
				}
			}


			byte[] sharedBuffer = new byte[100];
			int offset = 0;
			foreach (var sourceImage in images)
				offset += ceras.Serialize(sourceImage, ref sharedBuffer, offset);
			offset += ceras.Serialize(images, ref sharedBuffer, offset);

			int writtenLength = offset;

			List<Image> clonedImages = new List<Image>();
			offset = 0;

			for (var i = 0; i < images.Length; i++)
			{
				Image img = null;
				ceras.Deserialize(ref img, sharedBuffer, ref offset);
				clonedImages.Add(img);
			}
			Image[] imageArrayClone = null;
			ceras.Deserialize(ref imageArrayClone, sharedBuffer, ref offset);

			// Ensure all bytes consumed again
			Debug.Assert(offset == writtenLength);

			foreach (var img in clonedImages)
				img.Dispose();
			foreach (var img in imageArrayClone)
				img.Dispose();

		}
	}
}
