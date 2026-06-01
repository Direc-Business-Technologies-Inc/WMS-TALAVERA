using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.Platforms.Android;

public class TestBroadcastReceiver : BroadcastReceiver
{

    public void DWDecodeData(Intent _intent)
    {

        var jual = _intent.Extras.Get("com.symbol.datawedge.decode_data");

        var javaList = jual as JavaList;

        if (javaList != null)
        {
            for (int i = 0; i < javaList.Size(); i++)
            {
                byte[] bytes = (byte[])javaList.Get(i);
                foreach (var item in bytes)
                {
                    Log.Info("decode_data", "" + item);
                }
            }
        }

    }

    public override void OnReceive(Context context, Intent intent)
    {

        if (intent.Extras != null)
        {
            if (intent.HasExtra("com.symbol.datawedge.barcodes"))
            {
                List<Bundle> palobs = intent.Extras.GetParcelableArrayList("com.symbol.datawedge.barcodes").Cast<Bundle>().ToList();
                StringBuilder sb = new StringBuilder();
                foreach (Bundle b in palobs)
                {
                    String barcode = b.GetString("com.symbol.datawedge.data_string");
                    String timestamp = b.GetString("com.symbol.datawedge.timestamp");
                    String symbology = b.GetString("com.symbol.datawedge.label_type");
                    sb.AppendLine(barcode);
                }
                MauiProgram.BroadcastService.OnBroadcastReceived(sb.ToString());
            }
            else if (intent.HasExtra("com.symbol.datawedge.label_type"))
            {
                String bc_type = intent.Extras.GetString("com.symbol.datawedge.label_type");
                String bc_data = intent.Extras.GetString("com.symbol.datawedge.data_string");

                DWDecodeData(intent);

                MauiProgram.BroadcastService.OnBroadcastReceived(bc_data);

            }
        }
    }
}
