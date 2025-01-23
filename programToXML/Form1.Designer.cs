    using System.Drawing;
    using System.Windows.Forms;
    using static System.Net.Mime.MediaTypeNames;
    using System.Xml.Linq;

namespace programToXML
{
    partial class Form1
        {
            private System.ComponentModel.IContainer components = null;
            private System.Windows.Forms.Button btnSelectFile;
            private System.Windows.Forms.TextBox txtSourceFile;
            private System.Windows.Forms.Button btnExecute;
            private System.Windows.Forms.CheckBox nameCheckBox;

            protected override void Dispose(bool disposing)
            {
                if (disposing && (components != null))
                {
                    components.Dispose();
                }
                base.Dispose(disposing);
            }

        private void InitializeComponent()
        {
            btnSelectFile = new Button();
            txtSourceFile = new TextBox();
            btnExecute = new Button();
            nameCheckBox = new CheckBox();
            SuspendLayout();
            // 
            // btnSelectFile
            // 
            btnSelectFile.Location = new Point(35, 12);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(285, 27);
            btnSelectFile.TabIndex = 0;
            btnSelectFile.Text = "Выберите исходный файл";
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += btnSelectFile_Click;
            // 
            // txtSourceFile
            // 
            txtSourceFile.Location = new Point(51, 45);
            txtSourceFile.Name = "txtSourceFile";
            txtSourceFile.Size = new Size(250, 23);
            txtSourceFile.TabIndex = 1;
            // 
            // btnExecute
            // 
            btnExecute.Location = new Point(115, 101);
            btnExecute.Name = "btnExecute";
            btnExecute.Size = new Size(120, 23);
            btnExecute.TabIndex = 3;
            btnExecute.Text = "Выполнить";
            btnExecute.UseVisualStyleBackColor = true;
            btnExecute.Click += btnExecute_Click;
            // 
            // nameCheckBox
            // 
            nameCheckBox.Checked = true;
            nameCheckBox.CheckState = CheckState.Checked;
            nameCheckBox.Location = new Point(51, 75);
            nameCheckBox.Name = "nameCheckBox";
            nameCheckBox.Size = new Size(200, 20);
            nameCheckBox.TabIndex = 2;
            nameCheckBox.Text = "Заполнять пути";
            nameCheckBox.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            ClientSize = new Size(358, 136);
            Controls.Add(btnExecute);
            Controls.Add(nameCheckBox);
            Controls.Add(txtSourceFile);
            Controls.Add(btnSelectFile);
            Name = "Form1";
            Text = "Загрузчик файлов";
            ResumeLayout(false);
            PerformLayout();
        }
    }
    

}
