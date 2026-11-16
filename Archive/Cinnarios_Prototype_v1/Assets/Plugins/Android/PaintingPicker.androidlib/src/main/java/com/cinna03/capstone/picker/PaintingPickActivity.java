package com.cinna03.capstone.picker;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

public class PaintingPickActivity extends Activity {
    static final int RequestCode = 41;
    String target = "Artwork Placement";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        String extra = getIntent().getStringExtra("gameObject");
        if (extra != null && extra.length() > 0)
            target = extra;

        Intent pick = new Intent(Intent.ACTION_GET_CONTENT);
        pick.setType("image/*");
        pick.addCategory(Intent.CATEGORY_OPENABLE);
        startActivityForResult(pick, RequestCode);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        String path = "";
        if (requestCode == RequestCode && resultCode == RESULT_OK && data != null && data.getData() != null)
            path = copy(data.getData());
        UnityPlayer.UnitySendMessage(target, "OnPaintingPicked", path);
        finish();
    }

    String copy(Uri uri) {
        InputStream input = null;
        FileOutputStream output = null;
        try {
            input = getContentResolver().openInputStream(uri);
            if (input == null)
                return "";
            File file = new File(getCacheDir(), "capstone-painting.jpg");
            output = new FileOutputStream(file);
            byte[] buffer = new byte[8192];
            int read;
            while ((read = input.read(buffer)) != -1)
                output.write(buffer, 0, read);
            return file.getAbsolutePath();
        } catch (Exception ignored) {
            return "";
        } finally {
            try {
                if (output != null)
                    output.close();
                if (input != null)
                    input.close();
            } catch (Exception ignored) {
            }
        }
    }
}
