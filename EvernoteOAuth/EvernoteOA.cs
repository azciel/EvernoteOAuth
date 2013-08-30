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
 * Evernote 専用 OAuthクラス
 */
using System;
using System.Collections.Specialized;
using System.Text;
using System.IO;
using System.Net;
using System.Web;
using System.Windows.Forms;

namespace EvernoteOAuth {
    
    /// <summary>
    /// Evernote 専用 OAuthクラス
    /// </summary>
    /// <remarks>
    /// Evernote の OAuth 認証を実行する。Web 画面も表示する
    /// </remarks> 
    public class EvernoteOA {

        // Evernote API への URI Prefix
        private static readonly string EVERNOTE_URI_SANDBOX = @"https://sandbox.evernote.com";
        private static readonly string EVERNOTE_URI_PRODUCTION = @"https://www.evernote.com";

        // Evernote OAuth への URI
        private static readonly string REQUEST_TOKEN_URI = @"/oauth";
        private static readonly string AUTHORIZE_URI = @"/OAuth.action";

        // ダミーのコールバック URI
        private static readonly string DUMMY_CALLBACK_URI = @"_";

        // リクエストトークン URI
        private string requestTokenUri_;
        // 認証 URI
        private string authorizeUri_;

        // 一時クレデンシャル
        private string temp_credential_ = null;
        // 一時クレデンシャル署名
        private string oauth_verifier_ = null;

        // プロパティ要素
        private long edamExpires_;
        private string edamNoteStoreUri_;
        private string oauthToken_;
        private string edamUserId_;
        private string edamWebApiUriPrefix_;

        /// <summary>
        /// 開発用Sadboxかリリース用かのフラグ
        /// </summary>
        public enum HostService {
            Production = 0,
            Sandbox = 1
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="service">開発用Sadboxかリリース用かのフラグ</param>
        public EvernoteOA(HostService service) {
            string baseUri;
            if (service == HostService.Production) {
                baseUri = EVERNOTE_URI_PRODUCTION;
            } else {
                baseUri = EVERNOTE_URI_SANDBOX;
            }

            requestTokenUri_ = string.Format(@"{0}{1}",
                                             baseUri, REQUEST_TOKEN_URI);
            authorizeUri_ = string.Format(@"{0}{1}",
                                          baseUri, AUTHORIZE_URI);

            EdamExpires = 0;
            EdamNoteStoreUri = "";
            OAuthToken = "";
            EdamUserId = "";
            EdamWebApiUriPrefix = "";
        }

        /// <summary>
        /// セッションのタイムアウト時間
        /// </summary>
        public long EdamExpires {
            get {
                return edamExpires_;
            }
            set {
                edamExpires_ = value;
            }
        }

        /// <summary>
        /// NoteStore API の URI
        /// </summary>
        public string EdamNoteStoreUri {
            get {
                return edamNoteStoreUri_;
            }
            set {
                edamNoteStoreUri_ = value;
            }
        }

        /// <summary>
        /// OAuth Token
        /// </summary>
        public string OAuthToken {
            get {
                return oauthToken_;
            }
            set {
                oauthToken_ = value;
            }
        }

        /// <summary>
        /// ユーザーID (数値)
        /// </summary>
        public string EdamUserId {
            get {
                return edamUserId_;
            }
            set {
                edamUserId_ = value;
            }
        }

        /// <summary>
        /// WebAPI URI Prefix
        /// </summary>
        public string EdamWebApiUriPrefix {
            get {
                return edamWebApiUriPrefix_;
            }
            set {
                edamWebApiUriPrefix_ = value;
            }
        }

        // UNIX タイムスタンプ現在時刻取得
        private string GenerateTimeStamp() {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1,
                                                         0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        // ランダム文字列生成
        private string GenerateNonce() {
            int seed = Environment.TickCount;
            return new Random(seed).Next(123400, 9999999).ToString();
        }

        // リクエストトークン取得用 URI 生成
        private string createTokenUri(string consumerKey,
                                      string consumerSecret) {
            string nonce = GenerateNonce();
            string timestamp = GenerateTimeStamp();
            string signature = HttpUtility.UrlEncode(consumerSecret);

            StringBuilder sb = new StringBuilder();
            sb.Append(requestTokenUri_);
            sb.Append(@"?oauth_consumer_key=");
            sb.Append(consumerKey);
            sb.Append(@"&oauth_signature=");
            sb.Append(signature);
            sb.Append(@"&oauth_signature_method=PLAINTEXT");
            sb.Append(@"&oauth_timestamp=");
            sb.Append(timestamp);
            sb.Append(@"&oauth_nonce=");
            sb.Append(nonce);
            if (string.IsNullOrEmpty(temp_credential_)) {
                sb.Append(@"&oauth_callback=");
                sb.Append(DUMMY_CALLBACK_URI);
            } else {
                sb.Append(@"&oauth_token=");
                sb.Append(temp_credential_);
                sb.Append(@"&oauth_verifier=");
                if (!string.IsNullOrEmpty(oauth_verifier_)) {
                    sb.Append(oauth_verifier_);
                }
            }
            return sb.ToString();
        }

        // URI をコールして結果を取得する
        private string getWebRequest(string uri) {
            WebRequest webreq = WebRequest.Create(uri);
            webreq.Method = @"GET";
            WebResponse webres = webreq.GetResponse();
            Stream st = webres.GetResponseStream();
            StreamReader sr = new System.IO.StreamReader(st, Encoding.UTF8);
            string result = sr.ReadToEnd();
            result = result.Trim();

            return result;
        }

        // 認証 URI を生成
        private string createAuthUri(string token) {
            StringBuilder sb = new StringBuilder();
            sb.Append(authorizeUri_);
            sb.Append(@"?");
            sb.Append(token);
            sb.Append(@"&preferRegistration=true");
            return sb.ToString();
        
        }

        // ログイン用 Web ダイアログ表示
        private void showAuthDialog(string authUri, Form parentForm) {
            WebDialog dlg = (WebDialog)new WebDialog();
            dlg.AuthUri = authUri;
            dlg.ShowDialog(parentForm);
            dlg.Dispose();

            temp_credential_ = dlg.OAuthToken;
            oauth_verifier_ = dlg.OAuthVerifier;
        }

        // 認証結果をパースしてプロパティにセット
        private void parseCred(string cred) {
            cred = HttpUtility.UrlDecode(cred);
            NameValueCollection prms = HttpUtility.ParseQueryString(cred);

            EdamExpires = long.Parse(prms[@"edam_expires"]);
            EdamNoteStoreUri = prms[@"edam_noteStoreUrl"];
            OAuthToken = prms[@"oauth_token"];
            EdamUserId = prms[@"edam_userId"];
            EdamWebApiUriPrefix = prms[@"edam_webApiUrlPrefix"];
        }

        /// <summary>
        /// OAuth 認証実行
        /// </summary>
        /// <param name="consumerKey">Consumer Key</param>
        /// <param name="consumerSecret">ConsumerSecret</param>
        /// <param name="parentForm">認証 Web 画面の親フォーム</param>
        /// <returns>true:成功 / false:キャンセルなどにより不成功</returns>
        public bool doAuth(string consumerKey, string consumerSecret,
                           Form parentForm) {
            try {
                string tmpTokenUri = createTokenUri(consumerKey,
                                                    consumerSecret);
                string tmpToken = getWebRequest(tmpTokenUri);
                string authUri = createAuthUri(tmpToken);
                showAuthDialog(authUri, parentForm);
                string credUri = createTokenUri(consumerKey, consumerSecret);
                string cred = getWebRequest(credUri);
                parseCred(cred);

                bool result = !string.IsNullOrEmpty(OAuthToken) &&
                              !string.IsNullOrEmpty(EdamNoteStoreUri);
                return result;
            } catch (Exception ex) {
                throw ex;
            }
        }


        /// <summary>
        /// OAuth 認証実行
        /// </summary>
        /// <param name="consumerKey">Consumer Key</param>
        /// <param name="consumerSecret">ConsumerSecret</param>
        /// <returns>true:成功 / false:キャンセルなどにより不成功</returns>
        public bool doAuth(string consumerKey, string consumerSecret) {
            return doAuth(consumerKey, consumerSecret, null);
        }

        /// <summary>
        /// OAuth 認証実行 (static メソッド)
        /// </summary>
        /// <param name="service">開発用Sadboxかリリース用かのフラグ</param>
        /// <param name="consumerKey">Consumer Key</param>
        /// <param name="consumerSecret">Consumer Secret</param>
        /// <param name="parentForm">認証 Web 画面の親フォーム</param>
        /// <returns>認証結果オブジェクト</returns>
        public static EvernoteOA Auth(HostService service,
                                         string consumerKey,
                                         string consumerSecret,
                                         Form parentForm) {
            EvernoteOA auth = new EvernoteOA(service);
            auth.doAuth(consumerKey, consumerSecret, parentForm);
            return auth;
        }

        /// <summary>
        /// OAuth 認証実行 (static メソッド)
        /// </summary>
        /// <param name="service">開発用Sadboxかリリース用かのフラグ</param>
        /// <param name="consumerKey">Consumer Key</param>
        /// <param name="consumerSecret">Consumer Secret</param>
        /// <param name="parentForm">認証 Web 画面の親フォーム</param>
        /// <returns>認証結果オブジェクト</returns>
        public static EvernoteOA Auth(HostService service,
                                         string consumerKey,
                                         string consumerSecret) {
            return EvernoteOA.Auth(service,
                                      consumerKey, consumerSecret,
                                      null);
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
