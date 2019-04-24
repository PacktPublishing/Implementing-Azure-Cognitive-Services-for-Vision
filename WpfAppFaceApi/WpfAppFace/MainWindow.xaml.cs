using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.IO;
using System.Configuration;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;

namespace WpfAppFace
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        // You must use the same region as you used to get your subscription
        // keys. For example, if you got your subscription keys from westus,
        // replace "westcentralus" with "westus".
        //
        // Free trial subscription keys are generated in the westcentralus
        // region. If you use a free trial subscription key, you shouldn't
        // need to change the region.
        // Specify the Azure region
        private const string faceEndpoint =
            "https://<enter your location here>.api.cognitive.microsoft.com";

        private static readonly FaceAttributeType[] faceAttributes =
            { FaceAttributeType.Age, FaceAttributeType.Gender, FaceAttributeType.Emotion };

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string propertyName)
        {
            var handlers = PropertyChanged;

            handlers(this, new PropertyChangedEventArgs(propertyName));
        }

        private FaceClient FaceClient { get; set; }
        private bool _loadingImage1 = false;
        public bool LoadingImage1
        {
            get
            {
                return _loadingImage1;
            }
            private set
            {
                _loadingImage1 = value;
                RaisePropertyChanged("LoadingImage1");
            }
        }
        private bool _loadingImage2 = false;
        public bool LoadingImage2
        {
            get
            {
                return _loadingImage2;
            }
            private set
            {
                _loadingImage2 = value;
                RaisePropertyChanged("LoadingImage2");
            }
        }

        private object _personGroupListBoxSelectedItem;
        public object PersonGroupListBoxSelectedItem
        {
            get
            {
                return _personGroupListBoxSelectedItem;
            }
            set
            {
                _personGroupListBoxSelectedItem = value;
                RaisePropertyChanged("PersonGroupListBoxSelectedItemNotNull");
                RaisePropertyChanged("PersonGroupListBoxSelectedItem");
            }
        }

        private object _personListBoxSelectedItem;
        public object PersonListBoxSelectedItem
        {
            get
            {
                return _personListBoxSelectedItem;
            }
            set
            {
                _personListBoxSelectedItem = value;
                RaisePropertyChanged("PersonListBoxSelectedItemNotNull");
                RaisePropertyChanged("PersonListBoxSelectedItem");
            }
        }

        public bool PersonGroupListBoxSelectedItemNotNull
        {
            get
            {
                return PersonGroupListBoxSelectedItem != null;
            }
        }

        public bool PersonListBoxSelectedItemNotNull
        {
            get
            {
                return PersonListBoxSelectedItem != null;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            FaceClient = new FaceClient(new ApiKeyServiceClientCredentials(ConfigurationManager.AppSettings["apikey"]),
                                         new System.Net.Http.DelegatingHandler[] { })
            {
                Endpoint = faceEndpoint
            };

            InitPersonGroupListBox();
        }

        private async void InitPersonGroupListBox()
        {
            var personGroups = await FaceClient.PersonGroup.ListAsync();
            PersonGroupListBox.Items.Clear();
            foreach (var personGroup in personGroups)
            {
                PersonGroupListBox.Items.Add($"{personGroup.Name} ({personGroup.PersonGroupId})");
            }
        }

        private async void InitPersonListBox(string id)
        {
            PersonListBox.Items.Clear();
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var persons = await FaceClient.PersonGroupPerson.ListAsync(id);
            foreach (var person in persons)
            {
                PersonListBox.Items.Add($"{person.Name} ({person.PersonId})");
            }
            PersonName.Text = PersonData.Text = string.Empty;
        }

        private async void InitPersonDetailsListBox(string id, string gid)
        {
            PersonDetailListBox.Items.Clear();
            if (string.IsNullOrEmpty(id) ||
                string.IsNullOrEmpty(gid))
            {
                return;
            }

            var persons = await FaceClient.PersonGroupPerson.ListAsync(gid);
            var person = persons.FirstOrDefault(p => p.PersonId == Guid.Parse(id));
            if (person == null)
            {
                return;
            }

            PersonDetailListBox.Items.Add($"Data: {person.UserData}");
            foreach (var fid in person.PersistedFaceIds)
            {
                PersonDetailListBox.Items.Add($"Face: {fid}");
            }
        }

        private async void LoadingImage1_Click(object sender, RoutedEventArgs e)
        {
            // Load Image
            var openFileDialog = ImageDialogHelper.GetFileDialog();
            LoadingImage1 = true;
            if (openFileDialog.ShowDialog() == true)
            {
                ImageDialogHelper.LoadImage(img1, openFileDialog.FileName);
                await DetectLocalAsync(txtImage1, openFileDialog.FileName);
            }
            LoadingImage1 = false;
        }

        private async void LoadingImage2_Click(object sender, RoutedEventArgs e)
        {
            // Load Image
            LoadingImage2 = true;
            var openFileDialog = ImageDialogHelper.GetFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                ImageDialogHelper.LoadImage(img2, openFileDialog.FileName);
                await DetectLocalAsync(txtImage2, openFileDialog.FileName);
            }
            LoadingImage2 = false;
        }

        // Detect faces in a local image
        private async Task DetectLocalAsync(TextBlock txtBlock, string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                txtBlock.Text = $"Unable to open or read localImagePath: {imagePath}";
                return;
            }
            txtBlock.Text = string.Empty;
            try
            {
                using (Stream imageStream = File.OpenRead(imagePath))
                {
                    // Detect all faces in stream 
                    var faceList = await FaceClient.Face.DetectWithStreamAsync(imageStream, true, false, faceAttributes);

                    // We only need all IDs
                    var faceListIds = faceList.Select(f => (Guid)f.FaceId).ToList();
                    // Get all persongroups
                    var personGroups = await FaceClient.PersonGroup.ListAsync();

                    // Loop thru all persongroups
                    foreach (var personGroup in personGroups)
                    {
                        try
                        {

                            // Get ID list of all faces identified
                            var idResultList = await FaceClient.Face.IdentifyAsync(faceListIds, personGroup.PersonGroupId);
                            foreach (var idResult in idResultList)
                            {
                                foreach (var candidate in idResult.Candidates)
                                {
                                    var person = await FaceClient.PersonGroupPerson.GetAsync(personGroup.PersonGroupId, candidate.PersonId);
                                    txtBlock.Text += $"Candidate: {person.Name} ({personGroup.Name})\nScore: {candidate.Confidence.ToString("p1")}";
                                }
                            }
                        }
                        catch { }
                    }

                    
                }
            }
            catch (APIErrorException e)
            {
                txtBlock.Text = $"Error: {imagePath}: {e.Message}";
            }
        }

        private static string GetFaceAttributes(IList<DetectedFace> faceList)
        {
            string attributes = string.Empty;

            foreach (var face in faceList)
            {
                double? age = face.FaceAttributes.Age;
                string gender = face.FaceAttributes.Gender.ToString();
                string happiness = face.FaceAttributes.Emotion.Happiness.ToString();
                attributes += $"Gender {gender}  Age {age}  Happiness {happiness}\n";
            }

            return attributes;
        }

        private ICommand _deletePersonGroupCommand;
        public ICommand DeletePersonGroupCommand
        {
            get
            {
                return _deletePersonGroupCommand ?? (_deletePersonGroupCommand = new CommandHandler(() => DeletePersonGroup(), true));
            }
        }

        private ICommand _deletePersonCommand;
        public ICommand DeletePersonCommand
        {
            get
            {
                return _deletePersonCommand ?? (_deletePersonCommand = new CommandHandler(() => DeletePerson(), true));
            }
        }

        public async void DeletePersonGroup()
        {
            var id = StringHelper.GetIdFromText(PersonGroupListBox.SelectedItem.ToString()).FirstOrDefault();
            if (!string.IsNullOrEmpty(id))
            {
                await FaceClient.PersonGroup.DeleteAsync(id);
                // Train!
                await FaceClient.PersonGroup.TrainAsync(id);
            }
            InitPersonGroupListBox();
        }

        public async void DeletePerson()
        {
            if(PersonGroupListBox.SelectedItem == null)
            {
                return;
            }

            var gid = StringHelper.GetIdFromText(PersonGroupListBox.SelectedItem.ToString()).FirstOrDefault();
            var id = StringHelper.GetIdFromText(PersonListBox.SelectedItem.ToString()).FirstOrDefault();
            if (!string.IsNullOrEmpty(id))
            {
                await FaceClient.PersonGroupPerson.DeleteAsync(gid, Guid.Parse(id));
                // Train!
                await FaceClient.PersonGroup.TrainAsync(gid);
            }
            InitPersonListBox(gid);
        }

        private async void DialogHost_DialogPersonGroupClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            if (!Equals(eventArgs.Parameter, true))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(PersonGroupTextBoxId.Text) &&
                !string.IsNullOrWhiteSpace(PersonGroupTextBoxName.Text))
            {
                await FaceClient.PersonGroup.CreateAsync(PersonGroupTextBoxId.Text.ToLowerInvariant().Trim().Replace(" ", string.Empty), PersonGroupTextBoxName.Text.Trim());
            }
            InitPersonGroupListBox();
        }

        private async void DialogHost_DialogPersonClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            if (!Equals(eventArgs.Parameter, true))
            {
                return;
            }

            var gid = StringHelper.GetIdFromText(PersonGroupListBox.SelectedItem.ToString()).FirstOrDefault();

            if(string.IsNullOrEmpty(gid))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(PersonName.Text))
            {
                await FaceClient.PersonGroupPerson.CreateAsync(gid, PersonName.Text.Trim(), PersonData.Text.Trim());
            }
            InitPersonListBox(gid);
        }

        private void PersonGroupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PersonGroupListBox.SelectedItem == null)
            {
                return;
            }
            var id = StringHelper.GetIdFromText(PersonGroupListBox.SelectedItem.ToString()).FirstOrDefault();

            PersonDetailListBox.Items.Clear();
            InitPersonListBox(id);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void PersonListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PersonListBox.SelectedItem == null)
            {
                return;
            }

            var gid = StringHelper.GetIdFromText(PersonGroupListBox.SelectedItem.ToString()).FirstOrDefault();
            var id = StringHelper.GetIdFromText(PersonListBox.SelectedItem.ToString()).FirstOrDefault();
            InitPersonDetailsListBox(id, gid);            
        }

        private void PersonDetailListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PersonDetailListBox.SelectedItem == null)
            {
                return;
            }
        }

        private async void AddFaceToPerson_Click(object sender, RoutedEventArgs e)
        {
            var gid = StringHelper.GetIdFromText(PersonGroupListBox.SelectedItem.ToString()).FirstOrDefault();
            var id = StringHelper.GetIdFromText(PersonListBox.SelectedItem.ToString()).FirstOrDefault();

            // Load Image
            var openFileDialog = ImageDialogHelper.GetFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                using (Stream imageStream = File.OpenRead(openFileDialog.FileName))
                {
                    await FaceClient.PersonGroupPerson.AddFaceFromStreamAsync(gid, Guid.Parse(id), imageStream);
                }
            }

            // Train!
            await FaceClient.PersonGroup.TrainAsync(gid);

            InitPersonDetailsListBox(id, gid);
        }
    }
}
