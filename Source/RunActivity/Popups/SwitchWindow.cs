﻿// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;

namespace ORTS.Popups
{
	public class SwitchWindow : Window
	{
		const int SwitchImageSize = 32;

		Image SwitchForwards;
		Image SwitchBackwards;

#if NEW_SIGNALLING
        Image TrainDirection;
        Image ForwardEye;
        Image BackwardEye;
#endif

        static Texture2D SwitchStates;

		public SwitchWindow(WindowManager owner)
#if NEW_SIGNALLING
            : base(owner, Window.DecorationSize.X + (int)2.5 * SwitchImageSize, Window.DecorationSize.Y + 2 * SwitchImageSize, "Switch")
#else
            : base(owner, Window.DecorationSize.X + 2 * SwitchImageSize, Window.DecorationSize.Y + 2 * SwitchImageSize, "Switch")
#endif
		{
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            if (SwitchStates == null)
                SwitchStates = Owner.Viewer.RenderProcess.Content.Load<Texture2D>("SwitchStates");
        }

#if !NEW_SIGNALLING
		protected override ControlLayout Layout(ControlLayout layout)
		{
			var vbox = base.Layout(layout).AddLayoutVertical();
			{
				vbox.Add(SwitchForwards = new Image(vbox.RemainingWidth / 4, 0, vbox.RemainingWidth / 2, vbox.RemainingWidth / 2));
				vbox.Add(SwitchBackwards = new Image(vbox.RemainingWidth / 4, 0, vbox.RemainingWidth / 2, vbox.RemainingWidth / 2));
                SwitchForwards.Texture = SwitchBackwards.Texture = SwitchStates;
                SwitchForwards.Click += new Action<Control, Point>(SwitchForwards_Click);
				SwitchBackwards.Click += new Action<Control, Point>(SwitchBackwards_Click);
			}
			return vbox;
		}
#else
        protected override ControlLayout Layout(ControlLayout layout)
        {
            var hbox = base.Layout(layout).AddLayoutHorizontal();
            {
                var vbox1 = hbox.AddLayoutVertical(SwitchImageSize);
                vbox1.Add(ForwardEye = new Image(0, 0, SwitchImageSize, SwitchImageSize / 2));
                vbox1.Add(TrainDirection = new Image(0, 0, SwitchImageSize, SwitchImageSize));
                vbox1.Add(BackwardEye = new Image(0, 0, SwitchImageSize, SwitchImageSize / 2));

                var vbox2 = hbox.AddLayoutVertical(hbox.RemainingWidth);
                vbox2.Add(SwitchForwards = new Image(0, 0, SwitchImageSize, SwitchImageSize));
                vbox2.Add(SwitchBackwards = new Image(0, 0, SwitchImageSize, SwitchImageSize));
                SwitchForwards.Texture = SwitchBackwards.Texture = SwitchStates;
                SwitchForwards.Click += new Action<Control, Point>(SwitchForwards_Click);
                SwitchBackwards.Click += new Action<Control, Point>(SwitchBackwards_Click);
                TrainDirection.Texture = ForwardEye.Texture = BackwardEye.Texture = SwitchStates;
            }
            return hbox;
        }
#endif

		void SwitchForwards_Click(Control arg1, Point arg2)
		{
#if !NEW_SIGNALLING
            if( Owner.Viewer.Simulator.SwitchTrackAhead(Owner.Viewer.PlayerTrain) )
#else
            if (Owner.Viewer.Simulator.Signals.RequestSetSwitch(Owner.Viewer.PlayerTrain, Direction.Forward) )
#endif
            {
                new ToggleSwitchAheadCommand(Owner.Viewer.Log);
            }
		}

		void SwitchBackwards_Click(Control arg1, Point arg2)
		{
#if !NEW_SIGNALLING
            if( Owner.Viewer.Simulator.SwitchTrackBehind(Owner.Viewer.PlayerTrain) )
#else
            if ( Owner.Viewer.Simulator.Signals.RequestSetSwitch(Owner.Viewer.PlayerTrain, Direction.Reverse) )
#endif
            {
                new ToggleSwitchBehindCommand(Owner.Viewer.Log);
            }
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                var train = Owner.Viewer.PlayerTrain;
				try
				{
					UpdateSwitch(SwitchForwards, train, true);
					UpdateSwitch(SwitchBackwards, train, false);
				}
				catch (Exception) { }
#if NEW_SIGNALLING
                UpdateDirection(TrainDirection, train);
                UpdateEye(ForwardEye, train, true);
                UpdateEye(BackwardEye, train, false);
#endif
            }
        }

		void UpdateSwitch(Image image, Train train, bool front)
		{
			image.Source = new Rectangle(0, 0, SwitchImageSize, SwitchImageSize);

#if !NEW_SIGNALLING
            var traveller = front ^ train.LeadLocomotive.Flipped ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
#else
            var traveller = front ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
#endif
			TrackNode SwitchPreviousNode = traveller.TN;
			TrackNode SwitchNode = null;
			while (traveller.NextSection())
			{
				if (traveller.IsJunction)
				{
					SwitchNode = traveller.TN;
					break;
				}
				SwitchPreviousNode = traveller.TN;
			}
			if (SwitchNode == null)
				return;

			Debug.Assert(SwitchPreviousNode != null);
			Debug.Assert(SwitchNode.Inpins == 1);
			Debug.Assert(SwitchNode.Outpins == 2 || SwitchNode.Outpins == 3);  // allow for 3-way switch
			Debug.Assert(SwitchNode.TrPins.Count() == 3 || SwitchNode.TrPins.Count() == 4);  // allow for 3-way switch
			Debug.Assert(SwitchNode.TrJunctionNode != null);
			Debug.Assert(SwitchNode.TrJunctionNode.SelectedRoute == 0 || SwitchNode.TrJunctionNode.SelectedRoute == 1);

			var switchPreviousNodeID = Owner.Viewer.Simulator.TDB.TrackDB.TrackNodesIndexOf(SwitchPreviousNode);
			var switchBranchesAwayFromUs = SwitchNode.TrPins[0].Link == switchPreviousNodeID;
			var switchTrackSection = Owner.Viewer.Simulator.TSectionDat.TrackShapes.Get(SwitchNode.TrJunctionNode.ShapeIndex);  // TSECTION.DAT tells us which is the main route
			var switchMainRouteIsLeft = (int)switchTrackSection.MainRoute == 0;  // align the switch

			image.Source.X = ((switchBranchesAwayFromUs == front ? 1 : 3) + (switchMainRouteIsLeft ? 1 : 0)) * SwitchImageSize;
			image.Source.Y = SwitchNode.TrJunctionNode.SelectedRoute * SwitchImageSize;

#if NEW_SIGNALLING
            TrackCircuitSection switchSection = Owner.Viewer.Simulator.Signals.TrackCircuitList[SwitchNode.TCCrossReference[0].CrossRefIndex];
            if (switchSection.CircuitState.HasTrainsOccupying() || switchSection.CircuitState.SignalReserved >= 0 || 
                (switchSection.CircuitState.TrainReserved != null && switchSection.CircuitState.TrainReserved.Train.ControlMode != Train.TRAIN_CONTROL.MANUAL))
                image.Source.Y += 2 * SwitchImageSize;
#endif
		}

#if NEW_SIGNALLING
        void UpdateDirection(Image image, Train train)
        {
            image.Source = new Rectangle(0, 0, SwitchImageSize, SwitchImageSize);
            image.Source.Y = 4 * SwitchImageSize;
            image.Source.X = train.MUDirection == Direction.Forward ? 2 * SwitchImageSize :
                (train.MUDirection == Direction.Reverse ? 1 * SwitchImageSize : 0);
        }

        void UpdateEye(Image image, Train train, bool front)
        {
            image.Source = new Rectangle(0, 0, SwitchImageSize, SwitchImageSize / 2);
            image.Source.Y = (int)(4.25 * SwitchImageSize);
            bool flipped = train.LeadLocomotive.Flipped ^ train.LeadLocomotive.GetCabFlipped();
            image.Source.X = (front ^ !flipped) ? 0 : 3 * SwitchImageSize;
        }

#endif

	}
}
