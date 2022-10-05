using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
	/// <summary>
    /// Class script (default).
	/// Author: R. Dal Bello / 04.05.2022
    /// </summary>
    public class Script
    {
		public void Execute(ScriptContext context)
        {
			// Get the open plan 
			PlanSetup SelectedPlan = context.PlanSetup;
			
			// Get the beams of the plan
			IEnumerable<Beam> BeamsSelectedPlan = SelectedPlan.Beams;
			
			// Get the first beam in the list
			List<Beam> lBeamsSelectedPlan = BeamsSelectedPlan.ToList();
			Beam SelectedBeam = lBeamsSelectedPlan[0];
			
			// Loop over the gantry angle (control points) and extract patameters
			double MeanMlcOpening = 0;
			
			ControlPointCollection ControlPoints = SelectedBeam.ControlPoints;
			
			// Keep track of number of open leaves and the openings
			int NLeafsPairsOpen = 0;
            double LeafPairsOpenings = 0;
			
			for (int iControlPoint = 0; iControlPoint < ControlPoints.Count; iControlPoint++)
			{
				// Get the control point
				ControlPoint temp_ControlPoint = ControlPoints[iControlPoint];
				int temp_NLeafs = temp_ControlPoint.LeafPositions.GetLength(1);
				
				// Extract the opening
				for (int iLeaf = 1; iLeaf < temp_NLeafs; iLeaf++)
				{
					if (Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf] - temp_ControlPoint.LeafPositions[1, iLeaf]) > 0.01) // exclude leaf pairs that are closed
					{
						LeafPairsOpenings = LeafPairsOpenings + Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf] - temp_ControlPoint.LeafPositions[1, iLeaf]); // sum up the openings
						NLeafsPairsOpen++; // keep track of how many openings we have
					}
				}
				
			}
			
			// Average the leaf openings for this beam
            MeanMlcOpening = LeafPairsOpenings / NLeafsPairsOpen;
				
			
			// Print the output
			string PlanID = context.PlanSetup.Id;
			string FieldID = SelectedBeam.Id;
			MessageBox.Show("In the plan " + PlanID + " the field " + FieldID + " has MMO = " + MeanMlcOpening.ToString() + " mm");
			
			
		}
	}
}

		



