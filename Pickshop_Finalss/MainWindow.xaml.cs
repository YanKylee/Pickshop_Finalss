using System.Collections.ObjectModel;
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
using System.Globalization;



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
        string user = "";
        string userid = "";
        private ProductViewModel activeItem;
        private string selectedCateg = string.Empty;
        private string selectedAcceptCateg = string.Empty;

        private readonly ObservableCollection<ProductViewModel> _productViewModels;
        public IEnumerable<ProductViewModel> ProductViewModels => _productViewModels;
        NumberFormatInfo nfi = new NumberFormatInfo();
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                _dbConn = new DataClasses1DataContext(Properties.Settings.Default.PickShopDBConnectionString);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Server is under maintenance!");
                Application.Current.Shutdown();
            }

            _dbConn = new DataClasses1DataContext(Properties.Settings.Default.PickShopDBConnectionString);

            _productViewModels = new ObservableCollection<ProductViewModel>();

            _productViewModels.Clear();
            LoadProductsFromDatabase();

            DataContext = this;
           
            


        }

        private void Shopping_Click(object sender, RoutedEventArgs e)
        {
            _productViewModels.Clear();
            LoadProductsFromDatabase();
            itemName.Clear();
            itemCateg.Text = string.Empty;
            itemDesc.Clear();
            itemPrice.Clear();
            profile.Visibility = Visibility.Collapsed;
            SellingItems.Visibility = Visibility.Collapsed;
            listedItems.Visibility = Visibility.Visible;
        }

        private void Selling_Click(object sender, RoutedEventArgs e)
        {
            profile.Visibility = Visibility.Collapsed;
            listedItems.Visibility = Visibility.Collapsed;
            SellingItems.Visibility = Visibility.Visible;

        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            _productViewModels.Clear();
            LoadProductsByUserFromDatabase(userid);
            var userQuery = from s in _dbConn._Users
                            where s.User_Name.Trim() == user
                            select s;

            var userProfile = userQuery.FirstOrDefault();
            if (userProfile != null)
            {
                profileUser.Text = userProfile.User_Name;
                emailUser.Text = userProfile.User_Num;
                numberUser.Text = userProfile.User_Email;
                profile.Visibility = Visibility.Visible;
                listedItems.Visibility = Visibility.Collapsed;
                SellingItems.Visibility = Visibility.Collapsed;
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                listedItems.Visibility = Visibility.Collapsed;
                SellingItems.Visibility = Visibility.Collapsed;
                profile.Visibility = Visibility.Collapsed;
                login.Visibility = Visibility.Visible;
            }
            else
            {
                // Do not close the window  
            }
        }

        private void SignIn_click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(user_box.Text) || string.IsNullOrWhiteSpace(pass_box.Text))
            {
                MessageBox.Show("Please enter both username and password.");
                return;
            }

            try
            {
                flag = false;
                string messageString = "";
                IQueryable<_User> selectResults = from s in _dbConn._Users
                                                  where s.User_Name == user_box.Text
                                                  select s;
                if (selectResults.Count() == 1)
                {
                    foreach (_User s in selectResults)
                    {
                        if (s.User_Pass.Trim() == pass_box.Text)
                        {
                            mode = s.User_ID.Trim() == "U_2";

                            login.Visibility = Visibility.Collapsed;
                            listedItems.Visibility = Visibility.Visible;
                            user = s.User_Name;
                            userid = s.User_ID;
                            _productViewModels.Clear();
                            LoadProductsFromDatabase();
                            break;
                        }
                        else
                        {
                            messageString = "Incorrect Password. Try again!";
                            MessageBox.Show(messageString);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("User not found.");
                }

                user_box.Text = "";
                pass_box.Text = "";
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}");
            }
        }

        private void itemSell_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCateg != string.Empty)
            {
                MessageBox.Show($"Successfully Added!");
                var userQuery = from s in _dbConn._Users
                                where s.User_Name.Trim() == user
                                select s;

                var userProfile = userQuery.FirstOrDefault();
                Product nProduct = new Product();

                nProduct.Product_ID = GenerateitemID(GetitemID());
                nProduct.PType_ID = selectedCateg;
                nProduct.Product_Name = itemName.Text;
                nProduct.Product_Desc = itemDesc.Text;
                nProduct.Product_Quantity = "0";
                nProduct.Price = float.Parse(itemPrice.Text);
                nProduct.User_ID = userProfile.User_ID;
                _dbConn.Products.InsertOnSubmit(nProduct);
                _dbConn.SubmitChanges();
                selectedCateg = string.Empty;
                itemName.Text = string.Empty;
                itemDesc.Text = string.Empty;
                itemPrice.Text = string.Empty;
                itemCateg.Text = string.Empty;
                return;
            }
            MessageBox.Show($"Select product category");
        }

        private void itemCateg_SelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (itemCateg.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedCateg = selectedItem.Tag.ToString();
            }
        }

        private void acceptCateg_SelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (acceptCateg.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedAcceptCateg = selectedItem.Tag.ToString();
            }
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

                    var productViewModel = new ProductViewModel(product.Product_ID, product.PType_ID, product.Product_Name, product.Product_Desc, Convert.ToDouble(product.Price), product.User_ID);
                    _productViewModels.Add(productViewModel);
                }
            }
        }

        private void LoadProductsByUserFromDatabase(string userId)
        {
            

            IQueryable<Product> selectResults = from s in _dbConn.Products
                                                where s.User_ID == userId
                                                select s;

            if (selectResults.Any())
            {
                foreach (Product product in selectResults)
                {
                    var productViewModel = new ProductViewModel(
                        product.Product_ID,
                        product.PType_ID,
                        product.Product_Name,
                        product.Product_Desc,
                        Convert.ToDouble(product.Price),
                        product.User_ID
                    );
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            Button button = sender as Button;
            ProductViewModel item = button.DataContext as ProductViewModel;

            
            if (item != null)
            {
                NumberFormatInfo nfi = new NumberFormatInfo();
                nfi.CurrencySymbol = "₱";
                itemNameInfo.Text = item.Name;
                itemCategInfo.Text = getCategDesc(item.Category);
                itemDescInfo.Text = item.Description;
                itemPriceInfo.Text = item.Price.ToString("C",nfi);
                var userQuery = from u in _dbConn._Users
                                where u.User_ID == item.Userid
                                select u;

                var user = userQuery.FirstOrDefault();

                // Display the user's name and email
                    sellerInfo.Text = user.User_Name;
                    sellerContactInfo.Text = user.User_Email;
                


                listedItems.Visibility = Visibility.Collapsed;
                itemInfo.Visibility = Visibility.Visible;
            }
        }

        private void ButtonPart2_Click(object sender, RoutedEventArgs e)
        {

            Button button = sender as Button;
            activeItem = button.DataContext as ProductViewModel;


            if (activeItem != null)
            {
                NumberFormatInfo nfi = new NumberFormatInfo();
                nfi.CurrencySymbol = "₱";
                EditItemName.Text = activeItem.Name;
                EditItemCateg.Text = getCategDesc(activeItem.Category);
                selectedAcceptCateg = activeItem.Category;
                EditItemDesc.Text = activeItem.Description;
                EditItemPrice.Text = activeItem.Price.ToString("C", nfi);

                listedItems.Visibility = Visibility.Collapsed;
                profile.Visibility = Visibility.Collapsed;
                EditItem.Visibility = Visibility.Visible;

            }
        }

        private string getCategDesc(string categId)
        {
            switch (categId)
            {
                case "PT_1":
                    return "Electronics";

                case "PT_2":
                    return "Fashion";

                case "PT_3":
                    return "Home & Garden";

                case "PT_4":
                    return "Health & Beauty";

                case "PT_5":
                    return "Sports & Outdoors";

                case "PT_6":
                    return "Toys & Hobbies";

                case "PT_7":
                    return "Automotive" ;

                case "PT_8":
                    return "Books & Media" ;
            }

            return null;
        }

        private string getCategId(string categDesc)
        {
            switch (categDesc)
            {
                case "Electronics":
                    return "PT_1";

                case "Fashion":
                    return "PT_2";

                case "Home & Garden":
                    return "PT_3";

                case "Health & Beauty":
                    return "PT_4";

                case "Sports & Outdoors":
                    return "PT_5";

                case "Toys & Hobbies":
                    return "PT_6";

                case "Automotive":
                    return "PT_7";

                case "Books & Media":
                    return "PT_8";
            }

            return null;
        }







        private void backButton1_Click(object sender, RoutedEventArgs e)
        {
            itemInfo.Visibility = Visibility.Collapsed;
            listedItems.Visibility = Visibility.Visible;
            itemNameInfo.Text = "";
            itemCategInfo.Text = "";
            itemDescInfo.Text = "";
            itemPriceInfo.Text = "";
            sellerInfo.Text = "";
            sellerContactInfo.Text = "";
        }

        private void signupper(object sender, RoutedEventArgs e)
        {
            string username = "";
            string userpass = "";
            string useremail = "";
            string usernum = "";

            int usercount = 0;
            int productpricecount = 0;

            username = su_name.Text;
            userpass = su_pass.Text;
            useremail = su_email.Text;
            usernum = su_num.Text;

            IQueryable<_User> selectResults = from s in _dbConn._Users
                                              orderby s.User_ID
                                              select s;


            foreach (_User s in selectResults)
            {
                usercount++;
            }

            _User newUser = new _User();
            if (su_name.Text != "" && su_pass.Text != "" && su_email.Text != "" && su_num.Text != "")
            {
                newUser.User_ID = "U_" + (usercount + 1).ToString();
                newUser.User_Name = su_name.Text;
                newUser.User_Pass = su_pass.Text;
                newUser.User_Email = su_email.Text;
                newUser.User_Num = su_num.Text;
                _dbConn._Users.InsertOnSubmit(newUser);
                _dbConn.SubmitChanges();
                MessageBox.Show("Successfully registered!");
                signup.Visibility = Visibility.Collapsed;
                login.Visibility = Visibility.Visible;

                su_name.Text = "";
                su_pass.Text = "";
                su_email.Text = "";
                su_num.Text = "";
            }
            else
                MessageBox.Show("Please enter Correct data in all Text Boxes provided");

        }

        private void tosignup(object sender, RoutedEventArgs e)
        {
            login.Visibility = Visibility.Collapsed;
            signup.Visibility = Visibility.Visible;
        }

        private void tester(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("omg its working!");
        }

        private void DeleteItem_CLick(object sender, RoutedEventArgs e)
        {
            if (activeItem == null)
            {
                MessageBox.Show("No item selected to delete.");
                return;
            }

            // Show a confirmation dialog
            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete the item '{activeItem.Name}'?",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            // If the user clicked 'Yes', proceed with deletion
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Find the product in the database using the activeItem ID
                    var productQuery = from p in _dbConn.Products
                                       where p.Product_ID == activeItem.Itemid
                                       select p;

                    var productToDelete = productQuery.FirstOrDefault();

                    if (productToDelete != null)
                    {
                        // Delete the product
                        _dbConn.Products.DeleteOnSubmit(productToDelete);

                        // Submit changes to the database
                        _dbConn.SubmitChanges();
                        MessageBox.Show("Product deleted successfully!");

                        // Refresh the product list
                        _productViewModels.Clear();
                        LoadProductsByUserFromDatabase(userid);

                        // Update UI visibility
                        EditItem.Visibility = Visibility.Collapsed;
                        profile.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show("Product not found.");
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show($"Database error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred: {ex.Message}");
                }
            }
        }

        private void EditItem_CLick(object sender, RoutedEventArgs e)
        {

                NumberFormatInfo nfi = new NumberFormatInfo();
                nfi.CurrencySymbol = "₱";
                acceptName.Text = EditItemName.Text;
                //acceptCateg.Text = EditItemCateg.Text;
                
                acceptDesc.Text = EditItemDesc.Text;
                acceptPrice.Text = EditItemPrice.Text;

            foreach (ComboBoxItem item in acceptCateg.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == selectedAcceptCateg)
                {
                    acceptCateg.SelectedItem = item;
                    selectedAcceptCateg = item.Tag.ToString();
                    break;
                }
            }

            EditItem.Visibility = Visibility.Collapsed;
            AcceptEdit.Visibility = Visibility.Visible;

        }

        private void AcceptEdit_CLick(object sender, RoutedEventArgs e)
        {

            try
            {
                if (selectedAcceptCateg == string.Empty) 
                { 
                    MessageBox.Show("Select a Category"); 
                    return; 
                }
                // Find the product in the database using the activeItem ID
                var productQuery = from p in _dbConn.Products
                                   where p.Product_ID == activeItem.Itemid
                                   select p;

                var productToUpdate = productQuery.FirstOrDefault();

                if (productToUpdate != null)
                {
                    // Update product details
                    productToUpdate.Product_Name = acceptName.Text;
                    productToUpdate.PType_ID = selectedAcceptCateg;
                    productToUpdate.Product_Desc = acceptDesc.Text;

                    // Remove the peso sign if present
                    string priceText = acceptPrice.Text.Replace("₱", "").Trim();

                    // Validate and parse the price
                    if (float.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out float price))
                    {
                        productToUpdate.Price = price;
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid price.");
                        return;
                    }

                    // Submit changes to the database
                    _dbConn.SubmitChanges();
                    MessageBox.Show("Product updated successfully!");

                    // Refresh the product list
                    _productViewModels.Clear();
                    LoadProductsFromDatabase();
                    EditItemCateg.Text = getCategDesc(selectedAcceptCateg);
                    EditItemName.Text = acceptName.Text;
                    EditItemDesc.Text = acceptDesc.Text;
                    EditItemPrice.Text = acceptPrice.Text;
                    
                    AcceptEdit.Visibility = Visibility.Collapsed;
                    EditItem.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Product not found.");
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}");
            }
        }

        private void DeclineEdit_CLick(object sender, RoutedEventArgs e)
        { 
            AcceptEdit.Visibility = Visibility.Collapsed;
            EditItem.Visibility = Visibility.Visible;
            acceptName.Text = "";
            acceptCateg.Text = "";
            acceptDesc.Text = "";
            acceptPrice.Text = "";


        }

        private void EditBack_CLick(object sender, RoutedEventArgs e)
        {
            _productViewModels.Clear();
            LoadProductsByUserFromDatabase(userid);
            listedItems.Visibility = Visibility.Collapsed;
            itemInfo.Visibility = Visibility.Collapsed;
            EditItem.Visibility = Visibility.Collapsed;
            profile.Visibility = Visibility.Visible;
        }

    }
}

