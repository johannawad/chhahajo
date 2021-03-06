﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowSimulator
{
    [Serializable()]
    public partial class FlowNetworkSimulator : Form
    {

        Canvas canvas; // drawing area
        Point mousepoint; //point where mouse was clicked
        int selectedIndex;
        Part selectedPart, tempPart;
        Component selectedComponent, tempComponent;
        Point oldCoordinates; //old ccordinates of a component
        private Rectangle area;
        private ToolTip tooltip = new ToolTip();  // to show errors
        private bool compIsMoving = false, isOccupied = false, isSelected = false, addNewPipeline = false, connectedComp1 = false, wireIsSelected = false;
        Point first, second, third;
        Pipeline selectedPipeline, p;
        private ActionType action;
        private bool propertiesSet = true;
        private double percentage, remainingpercentage;
        private int maxFlow, currentFlow;
        FileHelper fh = new FileHelper();
        public FlowNetworkSimulator()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            canvas = new Canvas();
            area = new Rectangle(new Point(120, 95), new Size(550, 380));
            label8.Text = "Upper flow: " + 50 + "\n" + "Lower flow: " + 50;
            if (canvas.Components.Count == 0)
            {
                flowLabel.Enabled = false;
            }
            else
            {
                flowLabel.Enabled = true;
                
            }

        }

        private void btnPump_Click(object sender, EventArgs e)
        {
            selectedComponent = new Pump(new Point(0, 0));
            action = ActionType.Create;
        }



        private void moveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((selectedComponent = canvas.SelectComponent(mousepoint)) != null)
            {
                foreach (Component comp in canvas.Components)
                {
                    if (selectedComponent.SelectionArea.IntersectsWith(comp.SelectionArea))
                    {
                        oldCoordinates = comp.Position;
                        canvas.CreateUndo(ActionType.Move, selectedComponent);
                        canvas.Components.Remove(comp);
                        break;
                    }
                }
                isSelected = false;
                compIsMoving = true;
                this.Refresh();
            }
        }

        private void FlowNetworkSimulator_MouseClick(object sender, MouseEventArgs e)
        {
            flowLabel.Visible = false;
            mousepoint = PointToClient(Cursor.Position);
            tooltip.RemoveAll();
            IWin32Window win = this;
            if (e.Button == MouseButtons.Left)
            {

                isSelected = false;

                if ((tempComponent = canvas.SelectComponent(mousepoint)) != null)
                {
                    isSelected = true;

                    trackBar1.Enabled = false;
                    textBox1.Enabled = false;
                    textBox2.Enabled = false;
                    switch (tempComponent.GetType())
                    {
                        case ComponentType.Pipeline:
                            textBox2.Enabled = true;
                            break;
                        case ComponentType.Pump:
                            textBox1.Enabled = true;
                            textBox2.Enabled = true;
                            break;
                        case ComponentType.Splitter:
                            trackBar1.Enabled = true;
                            break;


                    }
                    fh.saved = false;
                    this.Refresh();


                }
                if (selectedComponent != null && !canvas.IsOverlapping(selectedComponent.SelectionArea) && area.IntersectsWith(new Rectangle(mousepoint, new Size(40, 40))))
                {
                    selectedComponent.Position = new Point(mousepoint.X, mousepoint.Y);
                    selectedComponent.UpdateSelectionArea();
                    canvas.CreateComponent(selectedComponent);
                    if(!compIsMoving)
                    canvas.CreateUndo(ActionType.Create, selectedComponent);
                    UndoButton.Enabled = true;
                    selectedComponent = null;
                    compIsMoving = false;
                    fh.saved = false;
                    this.Refresh();
                    this.Cursor = Cursors.Arrow;
                }



                if (selectedComponent != null && canvas.IsOverlapping(selectedComponent.SelectionArea) || !area.IntersectsWith(new Rectangle(mousepoint, new Size(40, 40))))
                {
                    
                    UndoButton.Enabled = true;
                    if (compIsMoving)
                    {
                        canvas.UndoRedoList.RemoveAt(canvas.UndoRedoIndex);
                        canvas.UndoRedoIndex--;
                        selectedComponent.Position = oldCoordinates;
                        selectedComponent.UpdateSelectionArea();
                        // if(selectedComponent)
                        canvas.CreateComponent(selectedComponent);
                        compIsMoving = false;
                        fh.saved = false;
                        this.Refresh();
                    }

                    selectedComponent = null;
                    //this.Cursor = Cursors.Arrow;
                    tooltip.Show("You cannot place a component here", win, mousepoint);
                    this.Cursor = Cursors.Arrow;

                }
                if (addNewPipeline)
                {
                    if (!connectedComp1)
                    {
                        first = new Point(mousepoint.X, mousepoint.Y);
                        connectedComp1 = true;

                    }
                    else
                    {
                        second = new Point(mousepoint.X, mousepoint.Y);
                        if (canvas.DrawPipeline(canvas.SelectComponent(first), canvas.SelectComponent(second)))
                        {
                            connectedComp1 = false;
                            addNewPipeline = false;


                            fh.saved = false;
                            this.Refresh();
                            this.Cursor = Cursors.Arrow;
                        }
                        else
                        {
                            tooltip.Show("Unable to establish a connection", win, Cursor.Position);
                            connectedComp1 = false;
                            fh.saved = false;
                            this.Cursor = Cursors.Arrow;
                            this.Refresh();
                        }
                    }
                }


            }
            else if (e.Button == MouseButtons.Right)
            {
                //  mousepoint = this.formPointToClient(Cursor.Position);
                //this.Cursor = Cursors.Arrow;


                if ((tempComponent = canvas.SelectComponent(mousepoint)) != null)
                {
                    isSelected = true;
                    this.Refresh();
                    cmsEdit.Show(Cursor.Position);

                }
                else if ((selectedPipeline = canvas.SelectPipeline(mousepoint)) != null)
                {

                    cmsEditPipeline.Show(Cursor.Position);
                    wireIsSelected = true;
                }
                else
                {
                    isSelected = false;
                }
                fh.saved = false;
                this.Refresh();
            }

            if (isSelected)
            {
                Component temp = canvas.SelectComponent(mousepoint);
                UpdateFlowLabel(temp);
                fh.saved = false;


            }
            if ((selectedPipeline = canvas.SelectPipeline(mousepoint)) != null)
            {

                UpdateFlowLabel(selectedPipeline);
                fh.saved = false;
            }

        }

        private void FlowNetworkSimulator_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (selectedComponent != null)
                {
                    Point mouseClick = this.PointToClient(Cursor.Position);
                   
                    selectedComponent.Position = new Point(mouseClick.X, mouseClick.Y);
                    selectedComponent.UpdateSelectionArea();
                    Rectangle tempRec = new Rectangle(mouseClick, new Size(40, 40));
                    if (canvas.IsOverlapping(tempRec) || !area.IntersectsWith(tempRec) && !propertiesSet)
                    {
                        this.Cursor = new Cursor(((Bitmap)((Part)selectedComponent).ComponentIconImage(false)).GetHicon());
                    }
                    else
                    {
                        this.Cursor = new Cursor(((Bitmap)((Part)selectedComponent).ComponentIconImage(true)).GetHicon());

                    }
                    if (compIsMoving)
                    {
                        selectedComponent.Position = new Point(mouseClick.X, mouseClick.Y);
                        Component temp = selectedComponent;
                        flowLabel.Location = new Point(temp.Position.X + 6, temp.Position.Y - 15);

                    }
                    this.Refresh();
                }
            }
            catch
            {
                selectedComponent = null;

            }
        }

        private void FlowNetworkSimulator_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                label2.Text = fh.Filename;
                Graphics gr = e.Graphics;
                if (canvas.Components != null)
                {
                    foreach (Component comp in canvas.Components)
                    {
                        if (comp is Part)
                            gr.DrawImage(((Part)comp).ComponentImage(true), comp.Position);
                        else
                        {
                            p = (Pipeline)comp;
                            if (p.IsCritical)
                            {
                                Pen pen1 = new Pen(Color.Red, 5);
                                gr.DrawLine(pen1, p.InPutPoint, p.OutPutPoint);
                            }
                            else
                            {

                                Pen pen1 = new Pen(Color.DeepSkyBlue, 5);
                                gr.DrawLine(pen1, p.InPutPoint, p.OutPutPoint);
                            }
                        }
                    }

                }
                if (isSelected)
                {
                    Pen pen = new Pen(Color.Gray, 3);
                    Rectangle rec = canvas.SelectComponent(mousepoint).SelectionArea;
                    gr.DrawRectangle(pen, rec);
                }



            }
            catch
            {
                isSelected = false;
            }


        }

        private void btnSink_Click(object sender, EventArgs e)
        {
            selectedComponent = new Sink(new Point(0, 0));
            action = ActionType.Create;
        }

        private void btnSplitter_Click(object sender, EventArgs e)
        {
            selectedComponent = new Splitter(new Point(0, 0));
            action = ActionType.Create;
        }

        private void btnMerger_Click(object sender, EventArgs e)
        {
            selectedComponent = new Merger(new Point(0, 0));
            action = ActionType.Create;
        }

        private void UndoButton_Click(object sender, EventArgs e)
        {
            canvas.UndoLastAction();
            if (canvas.UndoRedoIndex == -1)
            {
                UndoButton.Enabled = false;
            }
            RedoButton.Enabled = true;
            this.Refresh();

        }

        private void RedoButton_Click(object sender, EventArgs e)
        {
            if (canvas.UndoRedoIndex + 1 == canvas.UndoRedoList.Count)
            {
                RedoButton.Enabled = false;
            }
            else
                canvas.RedoLastAction();

            UndoButton.Enabled = true;
            this.Refresh();

        }

        private void btnPipe_Click(object sender, EventArgs e)
        {
            this.selectedComponent = null;
            if (canvas.Components.Count < 2)
            {
                IWin32Window win = this;
                tooltip.Show("Operation was canceled. Reason:\nYou must have at least two gates in your plain", win, Cursor.Position);
            }
            else
            {
                this.Cursor = Cursors.Cross;
                addNewPipeline = true;
                action = ActionType.Create;
            }
           
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            canvas.CreateUndo(ActionType.Delete, canvas.SelectComponent(mousepoint));
            canvas.DeletePipeline((Pipeline)canvas.SelectComponent(mousepoint));
            isSelected = false;
            this.Refresh();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            canvas.CreateUndo(ActionType.Delete, canvas.SelectComponent(mousepoint));
            if (canvas.SelectComponent(mousepoint) is Pipeline)
            {
                canvas.DeletePipeline((Pipeline)canvas.SelectComponent(mousepoint));
            }
            else
            {
                canvas.DeleteComponent(canvas.SelectComponent(mousepoint));
            }


            isSelected = false;
            this.Refresh();
        }
        private void textBox2_TextChanged(object sender, EventArgs e) // capacity
        {
            if ((selectedComponent = canvas.SelectComponent(mousepoint)) != null)
            {
                if (selectedComponent is Pump)
                {
                    if (textBox2.Text != "")
                    {
                        ((Pump)selectedComponent).Capacity = Convert.ToDouble(textBox2.Text);

                    }
                }
                if (selectedComponent is Pipeline)
                {
                    ((Pipeline)selectedComponent).SafetyLimit = Convert.ToDouble(textBox2.Text);




                }
                UpdateFlowLabel(selectedComponent);
            }
            selectedComponent = null;

            this.Refresh();

        }
        private void UpdateFlowLabel(Component temp)
        {
            flowLabel.Visible = true;
            if (temp is Pipeline)
                flowLabel.Location = canvas.getMidPoint((Pipeline)temp);
            else
                flowLabel.Location = new Point(temp.Position.X + 6, temp.Position.Y - 15);
            flowLabel.ForeColor = Color.Black;
            switch (temp.GetType())
            {
                case ComponentType.Pipeline:
                    if (((Pipeline)temp).SafetyLimit < temp.CurrentFlow)
                    {
                        flowLabel.ForeColor = Color.Red;
                    }
                    flowLabel.Text = "F: " + temp.CurrentFlow + " SL: " + ((Pipeline)temp).SafetyLimit;
                    break;
                case ComponentType.Pump:
                    flowLabel.Text = "F: " + temp.CurrentFlow + " C: " + ((Pump)temp).Capacity;
                    break;

                default:
                    flowLabel.Text = "Flow: " + temp.CurrentFlow;
                    break;
            }

        }
        private void textBox1_TextChanged(object sender, EventArgs e)//  flow
        {
            Component temp;

            if ((temp = canvas.SelectComponent(mousepoint)) != null)
            {
                if (textBox1.Text != "")
                {
                    temp.CurrentFlow = Convert.ToDouble(textBox1.Text);
                    UpdateFlowLabel(temp);
                    //canvas.UpdateProperties(temp);
                }
            }
            selectedComponent = null;

            this.Refresh();
        }
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {

            if ((selectedComponent = canvas.SelectComponent(mousepoint)) != null && selectedComponent is Splitter)
            {
                percentage = trackBar1.Value;
                remainingpercentage = trackBar1.Maximum - percentage;
                label8.Text = "Upperflow: " + percentage * 10 + "\n" + "Lowerflow: " + remainingpercentage * 10;

                ((Splitter)selectedComponent).PercentageUp = percentage / 10;
                ((Splitter)selectedComponent).PercentageDown = remainingpercentage / 10;
                //  canvas.UpdateProperties(selectedComponent);



            }
            selectedComponent = null;
            this.Refresh();

        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            Help help = new Help();
            help.ShowDialog();
        }
        private void btnPump_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Pump: Initiates the flow", win, mousepoint, 600);
        }
        private void btnSink_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Sink: Collects the flow", win, mousepoint, 600);
        }
        private void btnSplitter_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Splitter: Splits the flow", win, mousepoint, 600);
        }
        private void btnMerger_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Merger: Merges the flow", win, mousepoint, 600);
        }
        private void btnPipe_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Pipe: Establishes a connection", win, mousepoint, 600);
        }
        private void panel2_MouseHover(object sender, EventArgs e)
        {
            tooltip.RemoveAll();
        }
        private void NewButton_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("New", win, mousepoint, 400);
        }
        private void OpenButton_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Open", win, mousepoint, 400);
        }
        private void SaveButton_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Save", win, mousepoint, 400);
        }
        private void UndoButton_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Undo", win, mousepoint, 400);
        }
        private void RedoButton_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Redo", win, mousepoint, 400);
        }
        private void DeleteAllButton_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Delete all", win, mousepoint, 400);
        }
        private void HelpButton_MouseHover(object sender, EventArgs e)
        {
            IWin32Window win = this;
            Point mousepoint = PointToClient(Cursor.Position);
            tooltip.Show("Help", win, mousepoint, 400);
        }
        private void panel1_MouseHover(object sender, EventArgs e)
        {
            tooltip.RemoveAll();
        }
        private void panel4_MouseHover(object sender, EventArgs e)
        {
            tooltip.RemoveAll();
        }
        private void DeleteAllButton_Click(object sender, EventArgs e)
        {
            if (canvas.Components.Count > 0)
            {
                DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete your diagram? This action cannot be reverted.", "Delete All", MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    canvas.DeleteAll();
                    this.Refresh();

                }
            }

        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            if (canvas.Components.Count() == 0)
            {
                canvas = fh.LoadFromFile();
            }
            else
            {
                if (!fh.saved)
                {
                    DialogResult dr = MessageBox.Show("Do you want to save the current diagram before opening?",
                        "Save As", MessageBoxButtons.YesNoCancel);
                    if (dr == DialogResult.Yes)
                    {
                        fh.SaveToFile(canvas);
                        fh.saved = true;
                    }
                    if (dr == DialogResult.Cancel)
                    {
                        return;
                    }
                    if (dr == DialogResult.No)
                    {
                        canvas = fh.LoadFromFile();
                    }
                }

            }
            this.Refresh();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {

            if (canvas.Components.Count() != 0)
            {
                fh.SaveToFile(canvas);
            }
            else { MessageBox.Show("There are no components on the drawing area"); }
            this.Refresh();
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            if (!(canvas.Components.Count() == 0))
            {
                DialogResult dr = MessageBox.Show("Do you want to save the current diagram before opening?", "Save As", MessageBoxButtons.YesNo);
                if (dr.ToString() == "Yes")
                {
                    fh.SaveToFile(canvas);

                }
                canvas = new Canvas();

                this.Refresh();
            }
            this.Refresh();
        }
        /// <summary>
        /// Closes only if the drawing area is empty
        /// </summary>
        private void FlowNetworkSimulator_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {

                if (canvas.Components.Count() != 0 && !fh.saved)
                {
                    DialogResult dr = MessageBox.Show("Do you want to save the current diagram before quitting?", "Save As", MessageBoxButtons.YesNo);
                    if (dr.ToString() == "Yes")
                    {
                        fh.SaveToFile(canvas);

                    }
                }
            }
            catch
            {

            }
        }


    }
}

