using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HvZCommon;

namespace HvZClient
{
    class Resource
    {
        static readonly Dictionary<string, Resource> resources = new Dictionary<string, Resource>();
        private static readonly string nameSpace = @"Images/";
        private static string ext = ".png";

        public string Name { get; private set; }
        public BitmapImage Image { get; private set; }

        /// <summary>Image used in place of missing images</summary>
        static readonly Resource placeHolder = new Resource("holder", "holder");
        static readonly Resource humanTex = new Resource("human", "human");
        static readonly Resource zombieTex = new Resource("zombie", "zombie");
        static readonly Resource supplyPointTex = new Resource("supply", "supplyP");
        static readonly Resource sockDepoTex = new Resource("depo", "sockDepo");

        /// <summary>
        /// Container for client resources
        /// Currently only contains a bitmapImage
        /// </summary>
        public Resource(string name, string file, int posX = 0, int posY = 0, int width = -1, int height = -1)
        {
            Name = name;
            Image = makeBitmap(file);
            if (width > 0 && height > 0)
            {
                Image.SourceRect = new Int32Rect(posX, posY, width, height);
            }
            resources.Add(Name, this);
        }

        private static BitmapImage makeBitmap(string file)
        {
            file = nameSpace + Utils.validateFileName(file) + ext;

            return new BitmapImage(new Uri(file, UriKind.Relative));
        }

        /// <summary>
        /// Will be used by client for rendering objects from the map
        /// </summary>
        /// <param name="name">texture name returned from: ITakeSpace.TextureName</param>
        public static Resource getResourceByName(string name)
        {
            if (resources.ContainsKey(name))
            {
                return resources[name];
            }

            Debug.WriteLine("Resource not found: " + name);
            return placeHolder;
        }
    }
}
