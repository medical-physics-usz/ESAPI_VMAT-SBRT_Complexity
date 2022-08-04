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
	/// Author: R. Dal Bello / 03.03.2022
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
			double OverallTravelLeaves = 0;
			
			ControlPointCollection ControlPoints = SelectedBeam.ControlPoints;
			
			for (int iControlPoint = 1; iControlPoint < ControlPoints.Count; iControlPoint++)
                {
					// Get the control point and the previous one
                    ControlPoint temp_ControlPointNew = ControlPoints[iControlPoint];
                    ControlPoint temp_ControlPointOld = ControlPoints[iControlPoint-1];
					
					int temp_NLeafs = temp_ControlPointNew.LeafPositions.GetLength(1);
					
					// Get the leaf apertures
					for (int iLeaf = 1; iLeaf < temp_NLeafs; iLeaf++)
                    {
                        OverallTravelLeaves = OverallTravelLeaves + Math.Abs(temp_ControlPointNew.LeafPositions[0, iLeaf] - temp_ControlPointOld.LeafPositions[0, iLeaf]); // Bank B (X1) change wrt previous control point of the same leaf
                        OverallTravelLeaves = OverallTravelLeaves + Math.Abs(temp_ControlPointNew.LeafPositions[1, iLeaf] - temp_ControlPointOld.LeafPositions[1, iLeaf]); // Bank A (X2) change wrt previous control point of the same leaf

                    }
					
				}
				
			
			// Print the output
			string PlanID = context.PlanSetup.Id;
			string FieldID = SelectedBeam.Id;
			MessageBox.Show("In the plan " + PlanID + " the field " + FieldID + " has OTL = " + OverallTravelLeaves.ToString() + " mm");
			
			
		}
	}
}

		



