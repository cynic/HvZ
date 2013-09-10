using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HvZ.Common;

namespace HvZClient {
    public class Resource  {
        static readonly Dictionary<string, Resource> resources = new Dictionary<string, Resource>();
        private static readonly string nameSpace = @"/HvZClient;component/Images/";
        private static string ext = ".png";

        public string Name { get; private set; }
        public virtual BitmapImage Image { get; protected set; }

        /// <summary>Image used in place of missing images</summary>
        static readonly Resource placeHolder = new BasicResource("holder", "holder");
        static readonly Resource humanTex = new BasicResource("human", "human");
        static readonly Resource zombieTex = new BasicResource("zombie", "zombie");
        static readonly Resource supplyPointTex = new BasicResource("supply", "supplyP");
        static readonly EffectResource explosionTex = new EffectResource("explode", "boom");

        /// <summary>
        /// Container for client resources
        /// Currently only contains a bitmapImage
        /// </summary>
        protected Resource(string name, string file, int posX = 0, int posY = 0, int width = -1, int height = -1) {
            Name = name;
            Image = makeBitmap(file);
            if (width > 0 && height > 0) {
                Image.SourceRect = new Int32Rect(posX, posY, width, height);
            }
            resources.Add(Name, this);
        }

        private static BitmapImage makeBitmap(string file) {
            file = nameSpace + Utils.validateFileName(file) + ext;

            Uri uri = new Uri(file, UriKind.Relative);

            try {
                if (System.Windows.Application.GetResourceStream(uri) != null) {
                    return new BitmapImage(uri);
                }
            } catch (IOException) {
                Debug.WriteLine("Image not found: " + file);
            }

            return placeHolder.Image;
        }

        /// <summary>
        /// Will be used by client for rendering objects from the map
        /// </summary>
        /// <param name="name">texture name returned from: ITakeSpace.TextureName</param>
        public static Resource getResourceByName(string name) {
            if (resources.ContainsKey(name)) {
                return resources[name];
            }

            Debug.WriteLine("Resource not found: " + name);
            return placeHolder;
        }
    }

    class BasicResource : Resource {
        public BasicResource(string name, string file, int posX = 0, int posY = 0, int width = -1, int height = -1)
        : base(name, file, posX, posY, width, height) {
        }
    }

    class EffectResource : Resource {
        public override BitmapImage Image {
            get {
                BitmapImage result = base.Image;
                BitmapImage source = new BitmapImage();

                string[] rots = Enum.GetNames(typeof(Rotation));

                source.BeginInit();
                source.UriSource = result.UriSource;
                source.Rotation = (Rotation)Enum.Parse(typeof(Rotation), rots.PickOne(), true);
                source.EndInit();

                return source;
            }
            protected set {
                base.Image = value;
            }
        }

        public EffectResource(string name, string file, int posX = 0, int posY = 0, int width = -1, int height = -1)
        : base(name, file, posX, posY, width, height) {
        }
    }
}
