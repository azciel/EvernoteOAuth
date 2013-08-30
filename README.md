EvernoteOAuth
=============

Evernote 専用 AOuth 認証クラス (.Net)


使用例：

    EvernoteOA oauth = new EvernoteOA(EvernoteOA.HostService.Sandbox);
    if (oauth.doAuth(consumerKey, consumerSecret)) {
        // success;
        // oauth.OAuthToken OAuth <-- Access Token
        // oauth.EdamNoteStoreUri <-- NoteStore URI
    } else {
        // fail
    }

