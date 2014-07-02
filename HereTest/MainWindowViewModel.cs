using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Windows.Input;
using System.Xml;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

namespace HereTest
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Members and properties

        /// <summary>
        /// Web browser control
        /// </summary>
        System.Windows.Forms.WebBrowser browser;

        /// <summary>
        /// Counter for frames on webpage
        /// </summary>
        private int mFrameCount;

        /// <summary>
        /// busy indicator for UI
        /// </summary>
        private Boolean isBusy = true;

        /// <summary>
        /// Url text box binded string
        /// </summary>
        private string _url = string.Empty;
        public string URL
        {
            get { return _url; }
            set
            {
                _url = value;
                Uri myUri;
                if (Uri.TryCreate(_url, UriKind.Absolute, out myUri))
                    this.isBusy = false;
                else
                    this.isBusy = true;
                RaisePropertyChanged("URL");
            }
        }

        /// <summary>
        /// Result textbox binded string
        /// </summary>
        private string _result = string.Empty;
        public string Result
        {
            get { return _result; }
            set
            {
                _result = value;
                RaisePropertyChanged("Result");
            }
        }

        /// <summary>
        /// Command for getwords button
        /// </summary>
        private ICommand _getWords;
        public ICommand GetWords
        {
            get
            {
                if (null == _getWords)
                {
                    _getWords = new RelayCommand(() => this.ProcessUrl(), () =>
                    {
                        return !this.isBusy;
                    });
                }
                return _getWords;
            }
        }

        /// <summary>
        /// property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Functions

        /// <summary>
        /// Download webpage get all text and assign it to UI
        /// </summary>
        private void ProcessUrl()
        {
            this.isBusy = true;
            this.Result = string.Empty;
            try
            {
                browser = new System.Windows.Forms.WebBrowser();
                browser.ScriptErrorsSuppressed = true;
                browser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(browser_DocumentCompleted);
                mFrameCount = 0;
                browser.Navigate(new Uri(this.URL, UriKind.Absolute));
            }
            catch (Exception ex)
            {
                ShowOutput("ERROR");
                Trace.WriteLine(ex.StackTrace);
            }

            #region With HtmlAgilityPack DLL - webbrowser control can behave differently on different os's
            //WebClient client = new WebClient();
            //string content = client.DownloadString(new Uri(this.URL));
            //StringBuilder resultText = new StringBuilder();
            //HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            //doc.LoadHtml(content);
            //foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//text()"))
            //{
            //   resultText.Append( node.InnerText.Trim());
            //}
            #endregion
        }

        /// <summary>
        /// Document complete of web browser control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                Clipboard.Clear();
                if (((System.Windows.Forms.WebBrowser)sender).DocumentTitle.Contains("Navigation Canceled"))
                {
                    // unable to navigate
                    ShowOutput("Navigation Canceled");
                    return;
                }

                // check frames and update the result
                mFrameCount += 1;
                bool done = true;
                if (this.browser.Document != null)
                {
                    HtmlWindow win = this.browser.Document.Window;
                    if (win.Frames.Count > mFrameCount && win.Frames.Count > 0)
                        done = false;
                }
                if (done)
                {
                    this.Result = string.Empty;
                    string outputText = string.Empty;
                    ((System.Windows.Forms.WebBrowser)sender).Document.ExecCommand("SelectAll", false, null);
                    ((System.Windows.Forms.WebBrowser)sender).Document.ExecCommand("Copy", false, null);

                    //Remove all extra spaces and make all words single space apart
                    string copiedText = Clipboard.GetText().Trim();
                    copiedText = Regex.Replace(copiedText, "[\n\r\t]", " ");
                    copiedText = Regex.Replace(copiedText, @"\s+", " ");

                    if (string.IsNullOrEmpty(copiedText))
                    {
                        outputText = "EMPTY";
                    }
                    else
                    {
                        var resultList = copiedText.Split(' ')
                                         .GroupBy(x => x, StringComparer.InvariantCultureIgnoreCase) // Ignore case
                                         .Select(x => new { Word = x.Key, Occurance = x.Count() })
                                         .OrderByDescending(x => x.Occurance)
                                         .Take(5);

                        foreach (var member in resultList)
                        {
                            outputText = outputText + "[WORD]: " + member.Word.Trim() + " [COUNT]: " + member.Occurance + "\r";
                        }
                    }
                    ShowOutput(outputText);
                }
            }
            catch (Exception ex)
            {
                ShowOutput("ERROR");
                Trace.WriteLine(ex.StackTrace);
            }
        }

        private void ShowOutput(string resultText)
        {
            this.Result = resultText;
            this.isBusy = false;
        }

        /// <summary>
        /// Raise property event handler
        /// </summary>
        /// <param name="propertyName"></param>
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
