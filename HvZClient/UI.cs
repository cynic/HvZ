using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cb = System.Windows.Media.Brushes;

namespace HvZClient {
    static class Constants {
        internal const int PORT_SIZE = 11;
        internal const int TEX_SIZE = 44;
        internal static readonly Rect port = new Rect(12, 9, PORT_SIZE, PORT_SIZE);
    }

    public static class ButtonUtils {
        private static System.Windows.Media.Brush savedBackground;

        public static void setBoxes(Button r, Button m) {
            setRestoreBoxes(r);
            setMinimizeBoxes(m);
        }

        public static void setEditBoxes(Button e) { setBoxesGen(5, e); }
        public static void setPinBoxes(Button p) { setBoxesGen(3, p); }
        public static void setMinimizeBoxes(Button m) { setBoxesGen(0, m); }
        public static void setRestoreBoxes(Button r) { setBoxesGen(1, r); }

        public static void setBoxesGen(int left, Button m) { setBoxesGen(left, 0, m); }
        public static void setBoxesGen(int left, int top, Button m) {
            while (left >= 7) {
                top += 2;
                left -= 7;
            }

            ImageBrush b = brushFromButton(m);
            b.Viewbox = new Rect(Constants.TEX_SIZE * left, Constants.TEX_SIZE * top, Constants.TEX_SIZE, Constants.TEX_SIZE);
            b.Viewport = Constants.port;
        }

        public static void ShiftVert(int top, Button m) {
            ImageBrush b = ButtonUtils.brushFromButton(m);

            int oldTop = (int)b.Viewbox.Y / Constants.TEX_SIZE;

            if (oldTop % 2 != top % 2) {
                if (oldTop % 2 == 1) {
                    top = oldTop - 1;
                } else {
                    top = oldTop + 1;
                }
            }

            b.Viewbox = new Rect(b.Viewbox.X, top * Constants.TEX_SIZE, b.Viewbox.Width, b.Viewbox.Height);
        }

        public static ImageBrush brushFromButton(Button obj) {
            return ((ImageBrush)getElementFromButton(obj, "box"));
        }

        public static object getElementFromButton(Button obj, string name) {
            object o = obj.Template.FindName(name, obj);
            return o;
        }

        public static void RestoreButtonClicked(Window context, Button sender) {
            if (context.WindowState == WindowState.Normal) {
                context.WindowState = WindowState.Maximized;
            } else {
                context.WindowState = WindowState.Normal;
            }
        }

        public static void WindowMinimized(Window context, Button res) {
            buttonLostFocus(res);
            context.WindowState = WindowState.Minimized;
        }

        public static void WindowStateChanged(Window context, Button res) {
            if (context.WindowState == WindowState.Normal) {
                ButtonUtils.brushFromButton(res).Viewbox = new Rect(44, 0, Constants.TEX_SIZE, Constants.TEX_SIZE);
            } else {
                ButtonUtils.brushFromButton(res).Viewbox = new Rect(44, Constants.TEX_SIZE, Constants.TEX_SIZE, Constants.TEX_SIZE);
            }
        }

        public static void repaintButton(Button sender) {
            if (sender.IsMouseOver) {
                buttonLostFocus(sender);
                buttonGotFocus(sender);
            }
        }

        public static void buttonGotFocus(Button sender) {
            Grid g = ((Grid)ButtonUtils.getElementFromButton(sender, "backColor"));
            savedBackground = g.Background;
            g.Background = ((String)sender.Tag) == "close" ? Cb.DarkRed : ((String)sender.Tag) == "open" ? Cb.DarkGreen : Cb.DarkBlue;
        }

        public static void buttonLostFocus(Button sender) {
            ((Grid)ButtonUtils.getElementFromButton(sender, "backColor")).Background = savedBackground;
        }
    }
}
