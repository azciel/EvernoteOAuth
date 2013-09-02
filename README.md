EvernoteOAuth
=============

Evernote 専用 OAuth 認証クラス (.Net)
デスクトップアプリ用


使用例：

    EvernoteOA oauth = new EvernoteOA(EvernoteOA.HostService.Sandbox);
    if (oauth.doAuth(consumerKey, consumerSecret)) {
        // success;
        // oauth.OAuthToken       <-- Access Token
        // oauth.EdamNoteStoreUri <-- NoteStore URI
    } else {
        // fail
    }

