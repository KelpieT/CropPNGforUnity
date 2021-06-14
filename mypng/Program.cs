using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace mypng
{
	class Program
	{
		private const string bordersInMeta = "spriteBorder";
		private const string BORDERS_IN_META = "x: {0}, y: {1}, z: {2}, w: {3}";
		private const string PNG = ".png";
		private const string CROP_PNG = "_crop.png";
		private const string META = ".meta";
		private const string PERFECT_PNG = "_cropPerfect.png";
		private const string PERFECT_TXT = "_perf.txt";
		private const string SEMICON = "{";
		private const string BACK_SEMICON = "}";
		private const string HELP_COMAND = "-h";
		private const string PATH_PARAM = "-p";
		private const string CROP_BY_BORDERS_COMMAND = "-c";
		private const string EDIT_BORDERS_BY_OLD_META_COMAND = "-m";
		private const string CROP_OUT_BORDERS_COMAND = "-ca";
		private const string CREATE_PERFECT_COMAND = "-cp";
		private const string RESTORE_PERFECT_BORDERS_COMAND = "-rp";
		private const string Pattern = "[0-9]+";
		private const string LOG_SAVED = " was saved";
		private const string LOG_XYZW = "{0} {1} {2} {3}";
		private const string LOG_ERROR_EXIST_META = "Something meta not exist, or check file names(orig";
		private const string LOG_ERROR_CORUPT_META = "something wrong with meta of croped image";
		private const string LOG_META_SUCSESS = "meta Was Changed";
		private const string HELP_MESSAGE = "For help type \" {0}\"";
		private const string PATH_MESAGE = "need asign path like: (command) {0} (pathToFile)";
		static int x;
		static int y;
		static int z;
		static int w;
		static string path;
		public static int upBorder;
		public static int downBorder;
		public static int leftBorder;
		public static int rightBorder;
		public static int inside_upBorder;
		public static int inside_downBorder;
		public static int inside_leftBorder;
		public static int inside_rightBorder;


		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine(String.Format(HELP_MESSAGE, HELP_COMAND));
				Console.Read();
				return;
			}
			if (args[0] == HELP_COMAND)
				WriteHelp();

			int pathIndex = Array.IndexOf(args, PATH_PARAM);
			if (pathIndex == -1 || pathIndex + 1 >= args.Length)
			{
				Console.WriteLine(String.Format(PATH_MESAGE, PATH_PARAM));
				return;
			}
			path = args[pathIndex + 1];
			switch (args[0])
			{
				case CROP_BY_BORDERS_COMMAND:
					GenerateImage();
					break;
				case EDIT_BORDERS_BY_OLD_META_COMAND:
					EditCropedMeta();
					break;
				case CROP_OUT_BORDERS_COMAND:
					CropOutsideByAlpha();
					break;
				case CREATE_PERFECT_COMAND:
					CreatePerfectImage();
					break;
				case RESTORE_PERFECT_BORDERS_COMAND:
					ReplaceByPerfCropedMeta();
					break;
			}
		}
		private static void WriteHelp()
		{
			Console.WriteLine("Comands:");
			Console.WriteLine("{0} for generate crop image by {1} in meta", CROP_BY_BORDERS_COMMAND, bordersInMeta);
			Console.WriteLine("{0} for change croped meta image by {1} in main meta", EDIT_BORDERS_BY_OLD_META_COMAND, bordersInMeta);
			Console.WriteLine("{0} for generate croped image by alpha in out borders ",CROP_OUT_BORDERS_COMAND);
			Console.WriteLine("{0} for generate croped image by alpha in out borders and inside borders", CREATE_PERFECT_COMAND);
			Console.WriteLine("{0} for create borders in meta in perfect croped png", RESTORE_PERFECT_BORDERS_COMAND);
			Console.WriteLine("For commands needs path: {0} path", PATH_PARAM);
			Console.WriteLine("Example: {0} {1} pathToFile", CROP_BY_BORDERS_COMMAND, PATH_PARAM);
			Console.ReadLine();
			return;
		}

		private static void CreateImage(string path)
		{
			Rectangle[] rectsForCroped = new Rectangle[]
			{
				new Rectangle(0, 0, x, w),
				new Rectangle(x, 0, z, w),
				new Rectangle(0, w, x, y),
				new Rectangle(x, w, z, y),
			};
			Image curImage = Image.FromFile(path);
			Rectangle[] rectsCur = new Rectangle[]
			{
				new Rectangle(0, 0, x, w),
				new Rectangle(curImage.Width -z , 0, z, w),
				new Rectangle(0, curImage.Height - y , x, y),
				new Rectangle(curImage.Width -z, curImage.Height - y, z, y),
			};
			List<Image> picesImg = new List<Image>();
			string newPath = path.Replace(PNG, CROP_PNG);
			for (int i = 0; i < rectsCur.Length; i++)
			{
				Image pice = Image.FromFile(path);
				picesImg.Add(cropImage(pice, rectsCur[i]));
			}
			using (Bitmap totalBmp = new Bitmap(x + z, y + w))
			{

				for (int i = 0; i < rectsCur.Length; i++)
				{
					using (Graphics g = Graphics.FromImage(totalBmp))
					{
						g.DrawImage(picesImg[i], rectsForCroped[i]);
					}
				}
				totalBmp.Save(newPath, System.Drawing.Imaging.ImageFormat.Png);
			}
			Console.WriteLine(newPath + LOG_SAVED);
			Console.ReadLine();
		}

		private static string ReadMetaFile(string pathMeta)
		{
			string readText = File.ReadAllText(pathMeta);
			string[] lines = readText.Split('\n');
			string bord = lines.Where(xx => xx.IndexOf(bordersInMeta) > -1).ToArray()[0];
			MatchCollection matches = Regex.Matches(bord, Pattern);
			x = int.Parse(matches[0].ToString());
			y = int.Parse(matches[1].ToString());
			z = int.Parse(matches[2].ToString());
			w = int.Parse(matches[3].ToString());
			Console.WriteLine(LOG_XYZW, x, y, z, w);
			return readText;
		}

		private static Image cropImage(Image img, Rectangle cropArea)
		{
			Bitmap bmpImage = new Bitmap(img);
			return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
		}
		private static void GenerateImage()
		{
			ReadMetaFile(path + META);
			CreateImage(path);
		}
		private static void EditCropedMeta()
		{

			if (!File.Exists(path.Replace(CROP_PNG, PNG)) || !File.Exists(path))
			{
				Console.WriteLine(LOG_ERROR_EXIST_META);
				return;
			}
			ReadMetaFile(path.Replace(CROP_PNG, PNG));
			string readTextCroped = File.ReadAllText(path);
			string[] linesCroped = readTextCroped.Split('\n');
			int ind = -1;
			for (int i = 0; i < linesCroped.Length; i++)
			{
				if (linesCroped[i].IndexOf(bordersInMeta) > -1)
				{
					ind = i;
					break;
				}
			}
			if (ind == -1)
			{
				Console.WriteLine(LOG_ERROR_CORUPT_META);
				return;
			}
			int toReplace = linesCroped[ind].IndexOf(SEMICON);
			string removestring = linesCroped[ind].Remove(toReplace, linesCroped[ind].Length - toReplace);
			linesCroped[ind] = removestring;
			string newParams = SEMICON + String.Format(BORDERS_IN_META, x, y, z, w) + BACK_SEMICON;
			linesCroped[ind] += newParams;
			string newMeta = string.Join("\n", linesCroped);
			File.WriteAllText(path, newMeta);
			Console.WriteLine(LOG_META_SUCSESS);
			Console.ReadLine();
		}
		private static void ReplaceByPerfCropedMeta()
		{

			if (!File.Exists(path.Replace(PERFECT_PNG, PERFECT_TXT)) || !File.Exists(path))
			{
				Console.WriteLine(LOG_ERROR_EXIST_META);
				Console.ReadLine();
				return;
			}
			ReadMetaFile(path + META);
			string readTextCroped = File.ReadAllText(path + META);
			string[] linesCroped = readTextCroped.Split('\n');
			int ind = -1;
			for (int i = 0; i < linesCroped.Length; i++)
			{
				if (linesCroped[i].IndexOf(bordersInMeta) > -1)
				{
					ind = i;
					break;
				}
			}
			if (ind == -1)
			{
				Console.WriteLine(LOG_ERROR_CORUPT_META);
				Console.ReadLine();
				return;
			}
			int toReplace = linesCroped[ind].IndexOf(SEMICON);
			string removestring = linesCroped[ind].Remove(toReplace, linesCroped[ind].Length - toReplace);
			linesCroped[ind] = removestring;
			string newParams = File.ReadAllText(path.Replace(PERFECT_PNG, PERFECT_TXT));
			linesCroped[ind] += newParams;
			string newMeta = string.Join("\n", linesCroped);
			File.WriteAllText(path + META, newMeta);
			Console.WriteLine(LOG_META_SUCSESS);
			Console.ReadLine();
		}

		private static int MyClamp(int min, int max, int value)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}
		public static void CropOutsideByAlpha()
		{
			Bitmap curImage = new Bitmap(path);
			upBorder = GetUpBorder(curImage);
			downBorder = GetDownBorder(curImage);
			leftBorder = GetLeftBorder(curImage);
			rightBorder = GetRightBorder(curImage);
			Rectangle r = new Rectangle(leftBorder, upBorder, rightBorder - leftBorder, downBorder - upBorder);
			Image cur = cropImage(curImage, r);
			using (Bitmap newImg = new Bitmap(rightBorder - leftBorder, downBorder - upBorder))
			{
				using (Graphics g = Graphics.FromImage(newImg))
				{
					g.DrawImage(cur, 0, 0);
				}
				newImg.Save(path + PNG, System.Drawing.Imaging.ImageFormat.Png);
			}
		}
		public static void CreatePerfectImage()
		{
			NewCrop(rectangles());
		}
		public static Rectangle[] rectangles()
		{
			ReadMetaFile(path + META);
			Bitmap curImage = new Bitmap(path);
			upBorder = GetUpBorder(curImage);
			downBorder = GetDownBorder(curImage);
			leftBorder = GetLeftBorder(curImage);
			rightBorder = GetRightBorder(curImage);
			inside_upBorder = MyClamp(0, curImage.Height, GetUpInsideBorder(curImage, w));
			inside_downBorder = MyClamp(0, curImage.Height, GetDownInsideBorder(curImage, y));
			inside_leftBorder = MyClamp(0, curImage.Width, GetLeftInsideBorder(curImage, x));
			inside_rightBorder = MyClamp(0, curImage.Width, GetRightInsideBorder(curImage, z));
			Rectangle[] rectsCur = new Rectangle[]
			{
				  new Rectangle(leftBorder, upBorder, inside_leftBorder - leftBorder, inside_upBorder - upBorder),

				 new Rectangle(inside_rightBorder,upBorder , rightBorder - inside_rightBorder, inside_upBorder - upBorder),

				 new Rectangle(leftBorder, inside_downBorder, inside_leftBorder - leftBorder, downBorder - inside_downBorder),

				new Rectangle(inside_rightBorder, inside_downBorder, rightBorder - inside_rightBorder, downBorder - inside_downBorder)
			};
			x = rectsCur[0].Width;
			y = rectsCur[2].Height;
			z = rectsCur[1].Width;
			w = rectsCur[0].Height;
			string newParams = SEMICON + String.Format(BORDERS_IN_META, x, y, z, w) + BACK_SEMICON;
			File.WriteAllText(path.Replace(PNG, PERFECT_TXT), newParams);
			return rectsCur;

		}
		public static void NewCrop(Rectangle[] rectsCur)
		{
			Rectangle[] rectsForCroped = new Rectangle[] {
					new Rectangle(0, 0, rectsCur[0].Width, rectsCur[0].Height),

					new Rectangle(rectsCur[0].Width, 0, rectsCur[1].Width, rectsCur[1].Height),

					new Rectangle(0, rectsCur[0].Height, rectsCur[2].Width, rectsCur[2].Height),

					new Rectangle(rectsCur[0].Width, rectsCur[0].Height, rectsCur[3].Width, rectsCur[3].Height),

				};
			Image curImage = Image.FromFile(path);
			List<Image> picesImg = new List<Image>();
			string newPath = path.Replace(PNG, PERFECT_PNG);
			for (int i = 0; i < rectsCur.Length; i++)
			{
				Image pice = Image.FromFile(path);
				picesImg.Add(cropImage(pice, rectsCur[i]));
			}
			using (Bitmap totalBmp = new Bitmap(rectsCur[0].Width + rectsCur[1].Width, rectsCur[0].Height + rectsCur[2].Height))
			{

				for (int i = 0; i < rectsCur.Length; i++)
				{
					using (Graphics g = Graphics.FromImage(totalBmp))
					{
						g.DrawImage(picesImg[i], rectsForCroped[i]);
					}
				}
				totalBmp.Save(newPath, System.Drawing.Imaging.ImageFormat.Png);
			}
			Console.WriteLine(newPath + LOG_SAVED);
			Console.ReadLine();
		}
		private static int GetUpBorder(Bitmap curImage)
		{
			for (int i = 0; i < curImage.Height; i++)
			{
				for (int j = 0; j < curImage.Width; j++)
				{
					if (curImage.GetPixel(j, i).A > 0)
						return i;

				}
			}
			return -1;
		}
		private static int GetDownBorder(Bitmap curImage)
		{
			for (int i = curImage.Height - 1; i >= 0; i--)
			{
				for (int j = 0; j < curImage.Width; j++)
				{
					if (curImage.GetPixel(j, i).A > 0)
						return i + 1;
				}
			}
			return -1;
		}
		private static int GetLeftBorder(Bitmap curImage)
		{
			for (int j = 0; j < curImage.Width; j++)
			{
				for (int i = 0; i < curImage.Height; i++)
				{
					if (curImage.GetPixel(j, i).A > 0)
						return j;
				}
			}
			return -1;
		}
		private static int GetRightBorder(Bitmap curImage)
		{
			for (int j = curImage.Width - 1; j >= 0; j--)
			{
				for (int i = 0; i < curImage.Height; i++)
				{
					if (curImage.GetPixel(j, i).A > 0)
						return j + 1;
				}
			}
			return -1;
		}
		private static int GetUpInsideBorder(Bitmap curImage, int limit)
		{
			for (int i = limit; i > upBorder; i--)
			{
				for (int j = 0; j < curImage.Width; j++)
				{
					if (curImage.GetPixel(j, i) != curImage.GetPixel(j, i - 1)) return i;
				}
			}
			return -1;
		}
		private static int GetDownInsideBorder(Bitmap curImage, int limit)
		{
			for (int i = limit; i < downBorder; i++)
			{
				for (int j = 0; j < curImage.Width; j++)
				{
					if (curImage.GetPixel(j, i) != curImage.GetPixel(j, i + 1)) return i;
				}
			}
			return -1;
		}
		private static int GetLeftInsideBorder(Bitmap curImage, int limit)
		{

			for (int j = limit; j > leftBorder; j--)
			{
				for (int i = 0; i < curImage.Height; i++)
				{
					if (curImage.GetPixel(j, i) != curImage.GetPixel(j - 1, i)) return j;
				}
			}
			return -1;
		}
		private static int GetRightInsideBorder(Bitmap curImage, int limit)
		{
			for (int j = limit; j < rightBorder; j++)
			{
				for (int i = 0; i < curImage.Height; i++)
				{
					if (curImage.GetPixel(j, i) != curImage.GetPixel(j + 1, i)) return j;
				}
			}
			return -1;
		}
	}
}



