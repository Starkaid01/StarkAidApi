namespace StarkAid.WindowsForms.Forms;

partial class MainForm
{
    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        WB = new Microsoft.Web.WebView2.WinForms.WebView2();
        ((System.ComponentModel.ISupportInitialize)WB).BeginInit();
        SuspendLayout();
        // 
        // WB
        // 
        WB.AccessibleName = "";
        WB.AllowExternalDrop = true;
        WB.CreationProperties = null;
        WB.DefaultBackgroundColor = Color.White;
        WB.Location = new Point(0, 0);
        WB.Name = "WB";
        WB.Size = new Size(900, 459);
        WB.TabIndex = 0;
        WB.ZoomFactor = 1D;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 700);
        Controls.Add(WB);
        Name = "MainForm";
        ((System.ComponentModel.ISupportInitialize)WB).EndInit();
        ResumeLayout(false);
    }
    private Microsoft.Web.WebView2.WinForms.WebView2 WB;
}

