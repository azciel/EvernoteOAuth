/*
 * The MIT License (MIT)
 * 
 * Copyright (c) 2013 HAKKO Development Co.,Ltd. az'Ciel division
 * All Rights Reserved.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

/*
 * Evernote OAuth 認証用 Web 画面フォームクラス
 */

using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Web;

namespace EvernoteOAuth {

    /// <summary>
    /// Evernote OAuth 認証用 Web 画面フォームクラス
    /// </summary>
    public partial class WebDialog : Form {

        // プロパティ要素
        private string authUri_;
        private string oauthToken_;
        private string oauthVerifier_;

        /// <summary>
        /// 認証 URI
        /// </summary>
        public string AuthUri {
            set {
                authUri_ = value;
            }
        }

        /// <summary>
        /// OAuth Token
        /// </summary>
        public string OAuthToken {
            private set {
                oauthToken_ = value;
            }
            get {
                return oauthToken_;
            }
        }

        /// <summary>
        /// OAuth Verifier
        /// </summary>
        public string OAuthVerifier {
            private set {
                oauthVerifier_ = value;
            }
            get {
                return oauthVerifier_;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public WebDialog() {
            InitializeComponent();
        }

        // 画面表示ハンドラ
        private void WebDialog_Load(object sender, EventArgs e) {
            webBrowser.Navigate(authUri_);
        }

        // ブラウザ表示開始ハンドラ
        private void webBrowser_Navigating(object sender,
                                           WebBrowserNavigatingEventArgs e) {

            if (webBrowser.Url == null) {
                return;
            }
            if (string.IsNullOrEmpty(webBrowser.Url.Query)) {
                return;
            }

            Regex regex = new Regex(@"oauth_token=[^&]+.*oauth_verifier=[^&]+");
            Match m = regex.Match(webBrowser.Url.Query);
            if (m.Success) {
                string q = m.Value;
                NameValueCollection prm = HttpUtility.ParseQueryString(q);
                OAuthToken = prm[@"oauth_token"];
                OAuthVerifier = prm[@"oauth_verifier"];
                Close();
            }
        }

    }
}
/*
 * -*- settings for emacs. -*-
 * Local Variables:
 *   tab-width: 4
 *   indent-tabs-mode: nil
 *   c-basic-offset: 4
 * End:
 */
