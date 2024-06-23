﻿using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Data.Linq;
using System.Linq;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
using AForge.Video;
using AForge.Video.DirectShow;
using System.IO;
using System.Drawing;
using Pickshop_Finalss;
using System.Windows.Media.Media3D;



namespace Pickshop_Finalss
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {
        FilterInfoCollection fic = null;
        VideoCaptureDevice vcd = null;
        DataClasses1DataContext _dbConn = null;
        bool flag = false;
        bool mode = false; //false = user | true = moderator

        private readonly ObservableCollection<ProductViewModel> _productViewModels;
        public IEnumerable<ProductViewModel> ProductViewModels => _productViewModels;
        public MainWindow()
        {
            InitializeComponent();
            _dbConn = new DataClasses1DataContext(Properties.Settings.Default.PickShopDBConnectionString);

            _productViewModels = new ObservableCollection<ProductViewModel>()
            {
                //new ProductViewModel(@"C:\Users\Kyle\source\repos\TrueFinalsTerm\TrueFinalsTerm\Background\image.png","Tshirt", "Gucci replica", 100.00),
                //new ProductViewModel(@"C:\Users\Kyle\source\repos\TrueFinalsTerm\TrueFinalsTerm\Background\Tesla.png","Honda Civic", "120,000km", 10000000.00),
                //new ProductViewModel(@"C:\Users\Kyle\source\repos\TrueFinalsTerm\TrueFinalsTerm\Background\image.png","Alpha", "I dunno", 100.00),
                //new ProductViewModel(@"C:\Users\Kyle\source\repos\TrueFinalsTerm\TrueFinalsTerm\Background\gigachadd.png","Torotot", "Bruh", 3000.00),
                //new ProductViewModel(@"C:\Users\Kyle\source\repos\TrueFinalsTerm\TrueFinalsTerm\Background\image.png","Omega", "Test", 100.00),
                //new ProductViewModel(@"C:\Users\Kyle\source\repos\TrueFinalsTerm\TrueFinalsTerm\Background\image.png", "Adult Item", "The d", 3000.00),
                //new ProductViewModel(@"C:\Users\Kyle\source\repos\TrueFinalsTerm\TrueFinalsTerm\Background\image.png","Iphone 12 pro", "Used", 300.00)

            };
            //_productViewModels.Add(new ProductViewModel {Name = "kwkw"});

            DataContext = this;
            //this.WindowState = WindowState.Maximized;
            //this.WindowStyle = WindowStyle.None;
            //this.ResizeMode = ResizeMode.NoResize;


        }

        private void Shopping_Click(object sender, RoutedEventArgs e)
        {
            _productViewModels.Clear();
            LoadProductsFromDatabase();

            SellingItems.Visibility = Visibility.Collapsed;
            listedItems.Visibility = Visibility.Visible;
        }

        private void Selling_Click(object sender, RoutedEventArgs e)
        {
            listedItems.Visibility = Visibility.Collapsed;
            SellingItems.Visibility = Visibility.Visible;

        }

        private void Contact_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SignIn_click(object sender, RoutedEventArgs e)
        {
            if (user_box.Text.Length > 0 && pass_box.Text.Length > 0)
            {
                flag = false;
                string messageString = "";
                IQueryable<_User> selectResults = from s in _dbConn._Users
                                                  where s.User_Name.Trim() == user_box.Text
                                                  select s;
                if (selectResults.Count() == 1)
                {
                    //MessageBox.Show("works");
                    foreach (_User s in selectResults)
                    {
                        if (s.User_Pass.Trim() == pass_box.Text)
                        {
                            if (s.User_ID.Trim() == "U_1")
                            {
                                mode = false;
                            }
                            else if (s.User_ID.Trim() == "U_2")
                            {
                                mode = true;
                            }

                            if (!mode)
                            {
                                login.Visibility = Visibility.Collapsed;
                                listedItems.Visibility = Visibility.Visible;
                                messageString = s.User_Name + "\n" + s.User_Num + "\n" + s.User_Email + "\n" + s.User_ID;
                                MessageBox.Show(messageString);
                                _productViewModels.Clear();
                                LoadProductsFromDatabase();
                                break;
                            }
                            else
                            {

                                login.Visibility = Visibility.Collapsed;
                                listedItems.Visibility = Visibility.Visible;
                                messageString = s.User_Name + "\n" + s.User_Num + "\n" + s.User_Email + "\n" + s.User_ID;
                                MessageBox.Show(messageString);
                                _productViewModels.Clear();
                                LoadProductsFromDatabase();
                                break;
                            }

                        }
                        else if (s.User_Pass.Trim() != pass_box.Text)
                        {
                            messageString = "Incorrect Password.\n" +
                                "Try again!";
                            MessageBox.Show(messageString);
                        }
                    }


                }
            }



            //MessageBox.Show(user_box.Text + " " + pass_box.Text);
        }

        private void itemSell_Click(object sender, RoutedEventArgs e)
        {
            Product nProduct = new Product();

            nProduct.Product_ID = GenerateitemID(GetitemID());
            nProduct.PType_ID = "PT_1";
            nProduct.Product_Name = itemName.Text;
            nProduct.Product_Desc = itemDesc.Text;
            nProduct.Product_Quantity = "0";
            nProduct.Price = float.Parse(itemPrice.Text);

            _dbConn.Products.InsertOnSubmit(nProduct);
            _dbConn.SubmitChanges();

        }

        private string GenerateitemID(string highestID)
        {
            int num = int.Parse(highestID.Substring(4));
            num++;
            return "ITEM" + num.ToString("D3");
        }

        private string GetitemID()
        {
            var highestCust = _dbConn.Products.OrderByDescending(p => p.Product_ID).FirstOrDefault();
            if (highestCust != null)
            {
                return highestCust.Product_ID;
            }

            else
            {
                return "ITEM000";
            }
        }


        private void LoadProductsFromDatabase()
        {

            IQueryable<Product> selectResults = from s in _dbConn.Products
                                                select s;

            if (selectResults.Count() >= 1)
            {
                foreach (Product product in selectResults)
                {

                    var productViewModel = new ProductViewModel(product.PType_ID, product.Product_Name, product.Product_Desc, Convert.ToDouble(product.Price));
                    _productViewModels.Add(productViewModel);
                }
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            vcd = new VideoCaptureDevice(fic[cmbCamera.SelectedIndex].MonikerString);
            vcd.NewFrame += Vcd_NewFrame;
            vcd.Start();
        }


        private void Vcd_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            this.Dispatcher.Invoke(() =>
            {

                pic.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        ((Bitmap)eventArgs.Frame.Clone()).GetHbitmap(),
                        IntPtr.Zero,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight((int)pic.Width, (int)pic.Height));
            });

            //MessageBox.Show(eventArgs.Frame.Clone().ToString());
        }


        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            fic = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo fi in fic)
                cmbCamera.Items.Add(fi.Name);
            cmbCamera.SelectedIndex = 0;
            vcd = new VideoCaptureDevice();
        }


        public void ImageToFile(string filePath)
        {
            var image = pic.Source;
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)image));
                encoder.Save(fileStream);
            }
        }


        private void btnCaptureImage_Click(object sender, RoutedEventArgs e)
        {

            ImageToFile("Test.png");

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (vcd.IsRunning)
            {

                vcd = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Camera.Visibility = Visibility.Collapsed;
                SellingItems.Visibility = Visibility.Visible;

            }
        }

        private void itemPhoto_Click(object sender, RoutedEventArgs e)
        {
            SellingItems.Visibility = Visibility.Collapsed;
            Camera.Visibility = Visibility.Visible;
        }
    }
}

