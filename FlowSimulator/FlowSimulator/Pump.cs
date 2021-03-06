﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace FlowSimulator
{
    [Serializable()]
    public class Pump : Part
        
    {
        private double capacity;
        
        /// <summary>
        /// the capacity that is not allowed to be smaller than the CurrentFLow
        /// </summary>
        public double Capacity {
            get { return capacity; }
            set
            {
                if (value < 0)
                {
                    capacity = 0;
                }
                else
                {
                    capacity = value;
                }
            }
        }
        /// <summary>
        /// To establish a connection to the output
        /// </summary>
        /// <param name="pipeline"></param>
        /// <returns></returns>
        public override void Connect(Pipeline pipeline)
        {
            OutPut = pipeline;
        }

        public override void Disconnect(Pipeline p)
        {
            OutPut = null;
        }
        public Pump(Point position): base(position)
        {
            compImage = new Bitmap(Properties.Resources.pump);
            compImageNot = new Bitmap(Properties.Resources.pumpNot);
            compIcon = new Bitmap(Properties.Resources.pumpIco);
            compIconNot = new Bitmap(Properties.Resources.pumpNotIco);
            Capacity = 10;
        }
        public bool Input { get; set; }


        /// <summary>
        /// it gets it's Currentflow from the one that's stored and sets one that is in range of 0 - capacity
        /// </summary>
        public override double CurrentFlow
        {
            get
            {
                return currentflow;
            }

            set
            {
                if ((value < Capacity) && (value >= 0))
                {
                    currentflow = value;
                }else
                if (value <0)
                {
                    currentflow = 0;
                }
                else
                {
                    currentflow = Capacity;
                }
                
            }
        }

        public override Image ComponentImage(bool isNotOccupied)
        {
            if (isNotOccupied)
            {
                return compImage;
            }
            else return compImageNot;
        }

          public override Image ComponentIconImage(bool isNotOccupied)
          {
              if (isNotOccupied)
              {
                  return compIcon;
              }
              else return compIconNot;
          }

        public override Port WhichPort(Pipeline p)
        {
            
            return Port.OutPut;
        }
    }
}
