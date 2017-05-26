using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Utils;

namespace Popcorn.AttachedProperties
{
    /// <summary>
    /// Image async
    /// </summary>
    public class ImageAsyncHelper : DependencyObject
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Get source uri
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetImagePath(DependencyObject obj)
        {
            return (string) obj.GetValue(ImagePathProperty);
        }

        /// <summary>
        /// Set source uri
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetImagePath(DependencyObject obj, Uri value)
        {
            obj.SetValue(ImagePathProperty, value);
        }

        /// <summary>
        /// Image path property
        /// </summary>
        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.RegisterAttached("ImagePath",
                typeof(string),
                typeof(ImageAsyncHelper),
                new PropertyMetadata
                {
                    PropertyChangedCallback = (obj, e) =>
                    {
                        Task.Run(async () =>
                        {
                            var image = (Image) obj;
                            var resourceDictionary = new ResourceDictionary
                            {
                                Source = new Uri("Popcorn;component/Resources/ImageLoading.xaml", UriKind.Relative)
                            };

                            try
                            {
                                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                {
                                    var loadingImage = resourceDictionary["ImageLoading"] as DrawingImage;
                                    loadingImage.Freeze();

                                    #region Create Loading Animation

                                    var scaleTransform = new ScaleTransform(0.5, 0.5);
                                    var skewTransform = new SkewTransform(0, 0);
                                    var rotateTransform = new RotateTransform(0);
                                    var translateTransform = new TranslateTransform(0, 0);

                                    var group = new TransformGroup();
                                    group.Children.Add(scaleTransform);
                                    group.Children.Add(skewTransform);
                                    group.Children.Add(rotateTransform);
                                    group.Children.Add(translateTransform);

                                    var doubleAnimation =
                                        new DoubleAnimation(0, 359, new TimeSpan(0, 0, 0, 1))
                                        {
                                            RepeatBehavior = RepeatBehavior.Forever
                                        };

                                    rotateTransform.BeginAnimation(RotateTransform.AngleProperty, doubleAnimation);

                                    var loadingAnimationTransform = group;

                                    #endregion

                                    image.Source = loadingImage;
                                    image.RenderTransformOrigin = new Point(0.5, 0.5);
                                    image.RenderTransform = loadingAnimationTransform;
                                });

                                var path = e.NewValue as string;
                                if (string.IsNullOrEmpty(path))
                                {
                                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                    {
                                        var errorThumbnail = resourceDictionary["ImageError"] as DrawingImage;
                                        errorThumbnail.Freeze();
                                        image.RenderTransformOrigin = new Point(0.5d, 0.5d);
                                        var transformGroup = new TransformGroup();
                                        transformGroup.Children.Add(new ScaleTransform(0.5d, 0.5d));
                                        image.RenderTransform = transformGroup;
                                        image.Source = errorThumbnail;
                                    });
                                    return;
                                }

                                string localFile;
                                var fileName =
                                    path.Substring(path.LastIndexOf("/images/", StringComparison.InvariantCulture) +
                                                   1);
                                fileName = fileName.Replace('/', '_');
                                var files = FastDirectoryEnumerator.EnumerateFiles(Constants.Assets);
                                var file = files.FirstOrDefault(a => a.Name.Contains(fileName));
                                if (file != null)
                                {
                                    localFile = file.Path;
                                }
                                else
                                {
                                    using (var client = new HttpClient())
                                    {
                                        using (var ms = await client.GetStreamAsync(path))
                                        {
                                            using (var stream = new MemoryStream())
                                            {
                                                await ms.CopyToAsync(stream);
                                                using (var fs =
                                                    new FileStream(Constants.Assets + fileName, FileMode.OpenOrCreate,
                                                        FileAccess.ReadWrite, FileShare.ReadWrite))
                                                {
                                                    stream.WriteTo(fs);
                                                }
                                            }
                                        }
                                    }

                                    localFile = Constants.Assets + fileName;
                                }

                                using (var stream = File.Open(localFile, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                                    FileShare.ReadWrite))
                                {
                                    var bitmapImage = new BitmapImage();
                                    bitmapImage.BeginInit();
                                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmapImage.DecodePixelWidth = 400;
                                    bitmapImage.DecodePixelHeight = 600;
                                    bitmapImage.StreamSource = stream;
                                    bitmapImage.EndInit();
                                    bitmapImage.Freeze();

                                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                    {
                                        image.RenderTransformOrigin = new Point(0, 0);
                                        image.RenderTransform = new TransformGroup();
                                        image.Source = bitmapImage;
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                {
                                    var errorThumbnail = resourceDictionary["ImageError"] as DrawingImage;
                                    errorThumbnail.Freeze();
                                    image.RenderTransformOrigin = new Point(0.5d, 0.5d);
                                    var transformGroup = new TransformGroup();
                                    transformGroup.Children.Add(new ScaleTransform(0.5d, 0.5d));
                                    image.RenderTransform = transformGroup;
                                    image.Source = errorThumbnail;
                                });
                            }
                        });
                    }
                }
            );
    }
}