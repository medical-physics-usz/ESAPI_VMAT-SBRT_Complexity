using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace USZ_RtPlanAutomator.QA
{
    class MetricsCalc
    {

        // Calculate the overall leaf travel
        public static List<double> OTL(IEnumerable<Beam> Beams)
        {
            var retList = new List<double>();

            // Convert the beams to a list and then keep only the therapy fields (exclude setup fields)
            List<Beam> inBeams = Beams.ToList();
            List<Beam> therapyBeams = new List<Beam>();
            foreach (Beam beam in inBeams)
            {
                if (!beam.IsSetupField)
                {
                    therapyBeams.Add(beam);
                }
            }
            int NTherapyBeams = therapyBeams.Count();

            // OTL for each beam
            List<double> OtlTherapyBeams = new List<double>();
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                OtlTherapyBeams.Add(0);
            }


            // Go over the control points to calculate the overall leaf travel
            // Loop over the beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                // Get the control points collection
                ControlPointCollection temp_ControlPoints = null;
                temp_ControlPoints = therapyBeams[iBeam].ControlPoints;

                int NLeafsPairsOpen = 0;

                // Loop over the control points
                for (int iControlPoint = 1; iControlPoint < temp_ControlPoints.Count; iControlPoint++)
                {
                    // Get the control point and the previous one
                    ControlPoint temp_ControlPointNew = temp_ControlPoints[iControlPoint];
                    ControlPoint temp_ControlPointOld = temp_ControlPoints[iControlPoint-1];

                    // Perform calculations
                    int temp_NLeafs = temp_ControlPointNew.LeafPositions.GetLength(1);

                    
                    for (int iLeaf = 1; iLeaf < temp_NLeafs; iLeaf++)
                    {
                        OtlTherapyBeams[iBeam] = OtlTherapyBeams[iBeam] + Math.Abs(temp_ControlPointNew.LeafPositions[0, iLeaf] - temp_ControlPointOld.LeafPositions[0, iLeaf]); // Bank B (X1) change wrt previous control point of the same leaf
                        OtlTherapyBeams[iBeam] = OtlTherapyBeams[iBeam] + Math.Abs(temp_ControlPointNew.LeafPositions[1, iLeaf] - temp_ControlPointOld.LeafPositions[1, iLeaf]); // Bank A (X2) change wrt previous control point of the same leaf

                        if (Math.Abs(temp_ControlPointNew.LeafPositions[0, iLeaf] - temp_ControlPointNew.LeafPositions[1, iLeaf]) > 0.01) { NLeafsPairsOpen++; } // Keep track of how many leaf pairs are open

                    }
                }

                // Normalize the leaf travel by number of control points analyzed
                OtlTherapyBeams[iBeam] = OtlTherapyBeams[iBeam] / temp_ControlPoints.Count;

                // Normalize by the average number of opened leafs
                double AverageNLeafPairsOpen = NLeafsPairsOpen / temp_ControlPoints.Count;
                OtlTherapyBeams[iBeam] = OtlTherapyBeams[iBeam] / AverageNLeafPairsOpen;

            }

            // Calculate the total travel over all beams
            double OTL_Total = 0;
            foreach (double OTL in OtlTherapyBeams)
            {
                OTL_Total = OTL_Total + OTL;
            }

            // Normalize per Number beams, number banks (2), control points (done before)
            OTL_Total = OTL_Total / (NTherapyBeams * 2);


            // Prepare the list to be returned
            // Pos 0 = number of beams
            retList.Add(NTherapyBeams);

            // Pos 1 ... n = metric values for the single beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                retList.Add(OtlTherapyBeams[iBeam]);
            }

            // Pos n+1 = average of the metric over all the beams
            retList.Add(OTL_Total);

            return retList;
        }



        // Calculate mean leaf velocity
        public static List<double> MLV(IEnumerable<Beam> Beams)
        {
            var retList = new List<double>();

            // Convert the beams to a list and then keep only the therapy fields (exclude setup fields)
            List<Beam> inBeams = Beams.ToList();
            List<Beam> therapyBeams = new List<Beam>();
            foreach (Beam beam in inBeams)
            {
                if (!beam.IsSetupField)
                {
                    therapyBeams.Add(beam);
                }
            }
            int NTherapyBeams = therapyBeams.Count();

            // MLV for each beam
            List<double> MlvTherapyBeams = new List<double>();
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                MlvTherapyBeams.Add(0);
            }


            // Go over the control points to calculate the MLC motion speed
            // Loop over the beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                // Get the control points collection
                ControlPointCollection temp_ControlPoints = null;
                temp_ControlPoints = therapyBeams[iBeam].ControlPoints;
                double temp_LeafesAnalyzed = 0;

                // Loop over the control points
                for (int iControlPoint = 1; iControlPoint < temp_ControlPoints.Count; iControlPoint++)
                {
                    // Get the control point
                    ControlPoint temp_ControlPointNew = temp_ControlPoints[iControlPoint];
                    ControlPoint temp_ControlPointOld = temp_ControlPoints[iControlPoint-1];

                    // Perform calculations
                    int temp_NLeafs = temp_ControlPointNew.LeafPositions.GetLength(1);

                    for (int iLeaf = 0; iLeaf < temp_NLeafs; iLeaf++)
                    {
                        if (Math.Abs(temp_ControlPointNew.LeafPositions[0, iLeaf] - temp_ControlPointNew.LeafPositions[1, iLeaf]) > 0.01) // exclude leaf pairs that are closed
                        {
                            // too slow if using the function TimeBetweenControlPoints (passing arguments for each leaf...) implement directly here:

                            double temp_DeltaAngle = Math.Abs(temp_ControlPointOld.GantryAngle - temp_ControlPointNew.GantryAngle);
                            double temp_MU = Math.Abs(temp_ControlPointOld.MetersetWeight - temp_ControlPointNew.MetersetWeight);
                            temp_MU = temp_MU * temp_ControlPointNew.Beam.Meterset.Value; // MUs delivered in the conrol point
                            double MUperDeg = temp_DeltaAngle / temp_MU;

                            // implementation according to Spyridonidis Aristotelis from KSGR
                            double temp_TimeBetweenControlPoints = -99;
                            if (MUperDeg <= 1.6666666)
                            {
                                temp_TimeBetweenControlPoints = temp_DeltaAngle / 6.0f;
                            }
                            else if (MUperDeg > 1.6666666)
                            {
                                temp_TimeBetweenControlPoints = (temp_DeltaAngle / 6.0f) * (MUperDeg / 1.6666666f);
                            }

                            if (temp_TimeBetweenControlPoints <= 0)
                            {
                                temp_TimeBetweenControlPoints = -99; //error output
                            }


                            if (temp_TimeBetweenControlPoints > 0) // exclude error points in time calculation
                            {
                                MlvTherapyBeams[iBeam] = MlvTherapyBeams[iBeam] + Math.Abs(temp_ControlPointNew.LeafPositions[0, iLeaf] - temp_ControlPointOld.LeafPositions[0, iLeaf]) / temp_TimeBetweenControlPoints;
                                temp_LeafesAnalyzed = temp_LeafesAnalyzed + 1;

                            }
                        }

                    }
                }


                // Average the leaf velocity for the beam over the number of leafes that were analyzed
                if (temp_LeafesAnalyzed > 0)
                {
                    MlvTherapyBeams[iBeam] = MlvTherapyBeams[iBeam] / temp_LeafesAnalyzed;
                }

            }

            // Calculate the total mean leaf velocity scaling on MUs delivered per beam
            double MLV_Total = 0;
            double MU_Total = 0;
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                MLV_Total = MLV_Total + MlvTherapyBeams[iBeam] * therapyBeams[iBeam].Meterset.Value;
                MU_Total = MU_Total + therapyBeams[iBeam].Meterset.Value;
            }

            MLV_Total = MLV_Total / MU_Total; // normalize beams per total MUs after weighting per MUs delivered per beam


            // Prepare the list to be returned
            // Pos 0 = number of beams
            retList.Add(NTherapyBeams);

            // Pos 1 ... n = metric values for the single beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                retList.Add(MlvTherapyBeams[iBeam]);
            }

            // Pos n+1 = average of the metric over all the beams
            retList.Add(MLV_Total);

            return retList;
        }


        // Calculate index of modulation (from MLC opening --> perimeter / area)
        public static List<double> IOM(IEnumerable<Beam> Beams)
        {
            var retList = new List<Double>();

            // Convert the beams to a list and then keep only the therapy fields (exclude setup fields)
            List<Beam> inBeams = Beams.ToList();
            List<Beam> therapyBeams = new List<Beam>();
            foreach (Beam beam in inBeams)
            {
                if (!beam.IsSetupField)
                {
                    therapyBeams.Add(beam);
                }
            }
            int NTherapyBeams = therapyBeams.Count();

            // OTL for each beam
            List<double> IomTherapyBeams = new List<double>();
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                IomTherapyBeams.Add(0);
            }

            // Go over the control points to calculate the index of modulation
            // Loop over the beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                // Get the control points collection
                ControlPointCollection temp_ControlPoints = null;
                temp_ControlPoints = therapyBeams[iBeam].ControlPoints;

                // Loop over the control points
                for (int iControlPoint = 1; iControlPoint < temp_ControlPoints.Count; iControlPoint++)
                {
                    // Get the control point
                    ControlPoint temp_ControlPoint = temp_ControlPoints[iControlPoint];

                    // Perform calculations
                    int temp_NLeafs = temp_ControlPoint.LeafPositions.GetLength(1); 

                    // Calculate area of the opening for this control point
                    double temp_A = 0;
                    for (int iLeaf = 0; iLeaf < temp_NLeafs; iLeaf++)
                    {
                        temp_A = temp_A + Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf] - temp_ControlPoint.LeafPositions[1, iLeaf])*Tools.GetLeafWidth(temp_ControlPoint,iLeaf) ; // how much leaf from bank A is away from leaf bank B *times* leaf width
                    }

                    

                    // Calculate the perimeter for the opening
                    double temp_P = 0;
                    for (int iLeaf = 1; iLeaf < temp_NLeafs; iLeaf++)
                    {
                        if (Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf] - temp_ControlPoint.LeafPositions[1, iLeaf]) > 0.01 ) // exclude leaf pairs that are closed
                        {
                            // calculate the exposed edges for this segment
                            double temp_ExposedEdgeB = 0;
                            double temp_ExposedEdgeA = 0;

                            // exposed leaf edge from the previous leaf in bank B
                            temp_ExposedEdgeB = temp_ExposedEdgeB + Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf] - temp_ControlPoint.LeafPositions[0, iLeaf - 1]); 
                            // remove unexposed edges due to overtravel adjacent leafs
                            if (temp_ControlPoint.LeafPositions[1, iLeaf - 1] < temp_ControlPoint.LeafPositions[0, iLeaf]) { temp_ExposedEdgeB = temp_ExposedEdgeB - Math.Abs(temp_ControlPoint.LeafPositions[1, iLeaf - 1] - temp_ControlPoint.LeafPositions[0, iLeaf]); }
                            if (temp_ControlPoint.LeafPositions[1, iLeaf] < temp_ControlPoint.LeafPositions[0, iLeaf]) { temp_ExposedEdgeB = temp_ExposedEdgeB - Math.Abs(temp_ControlPoint.LeafPositions[1, iLeaf] - temp_ControlPoint.LeafPositions[0, iLeaf]); }

                            // exposed leaf edge from the previous leaf in bank A
                            temp_ExposedEdgeA = temp_ExposedEdgeA + Math.Abs(temp_ControlPoint.LeafPositions[1, iLeaf] - temp_ControlPoint.LeafPositions[1, iLeaf - 1]);
                            // remove unexposed edges due to overtravel adjacent leafs
                            if (temp_ControlPoint.LeafPositions[0, iLeaf] > temp_ControlPoint.LeafPositions[1, iLeaf - 1]) { temp_ExposedEdgeA = temp_ExposedEdgeA - Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf] - temp_ControlPoint.LeafPositions[1, iLeaf - 1]); } 
                            if (temp_ControlPoint.LeafPositions[0, iLeaf - 1] > temp_ControlPoint.LeafPositions[1, iLeaf]) { temp_ExposedEdgeA = temp_ExposedEdgeA - Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf - 1] - temp_ControlPoint.LeafPositions[1, iLeaf]); }

                            // update the perimeter with exposed edges + leaf width
                            temp_P = temp_P + temp_ExposedEdgeA + temp_ExposedEdgeB;
                            temp_P = temp_P + 2 * Tools.GetLeafWidth(temp_ControlPoint, iLeaf); // width of the leaf pair that is opened (x2 because bank A+B), different for HDMLC or Millennium MLC

                            // just some warning messages if calculation returns wrong values
                            if (temp_ExposedEdgeA < 0 || temp_ExposedEdgeB < 0 || Tools.GetLeafWidth(temp_ControlPoint, iLeaf) < 0 ) { System.Windows.MessageBox.Show("!! Error in MetricCalc/IOM !! \nExposedEdgeA = " + temp_ExposedEdgeA.ToString() + "\nExposedEdgeB = " + temp_ExposedEdgeB.ToString() + "\nLeafWidth = " + Tools.GetLeafWidth(temp_ControlPoint, iLeaf).ToString() + "\nCP, Beam = " + iControlPoint.ToString() + " " + temp_ControlPoint.Beam.Id ); }
                        }
                    }

                    // Calculate and store the IOM for this point
                    if (temp_A > 0)
                    {
                        IomTherapyBeams[iBeam] = IomTherapyBeams[iBeam] + temp_P / temp_A * Math.Abs(temp_ControlPoints[iControlPoint].MetersetWeight - temp_ControlPoints[iControlPoint - 1].MetersetWeight);  // normalize by MUs delivered by this control point (Meterset weight is already cumulative and normlized to 1)

                    } else
                    {
                        if (iControlPoint == 1) { System.Windows.MessageBox.Show("!! Error in MetricsCals/IOM !! \ntemp_A = 0 "); }
                    }

                }
            }

            // Calculate the total index of modulation
            double IOM_Total = 0;
            double MU_Total = 0;
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                IOM_Total = IOM_Total + IomTherapyBeams[iBeam] * therapyBeams[iBeam].Meterset.Value;
                MU_Total = MU_Total + therapyBeams[iBeam].Meterset.Value;
            }

            IOM_Total = IOM_Total / MU_Total; // normalize beams per total MUs after weighting per MUs delivered per beam


            // Prepare the list to be returned
            // Pos 0 = number of beams
            retList.Add(NTherapyBeams);

            // Pos 1 ... n = metric values for the single beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                retList.Add(IomTherapyBeams[iBeam]);
            }

            // Pos n+1 = average of the metric over all the beams
            retList.Add(IOM_Total);

            return retList;
        }


        // Calculate the mean MLC opening
        public static List<double> MMO(IEnumerable<Beam> Beams)
        {
            var retList = new List<Double>();

            // Convert the beams to a list and then keep only the therapy fields (exclude setup fields)
            List<Beam> inBeams = Beams.ToList();
            List<Beam> therapyBeams = new List<Beam>();
            foreach (Beam beam in inBeams)
            {
                if (!beam.IsSetupField)
                {
                    therapyBeams.Add(beam);
                }
            }
            int NTherapyBeams = therapyBeams.Count();

            // MMO for each beam
            List<double> MmoTherapyBeams = new List<double>();
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                MmoTherapyBeams.Add(0);
            }


            // Go over the control points to calculate the mean MLC opening
            // Loop over the beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                // Get the control points collection
                ControlPointCollection temp_ControlPoints = null;
                temp_ControlPoints = therapyBeams[iBeam].ControlPoints;

                int NLeafsPairsOpen = 0;
                double LeafPairsOpenings = 0;

                // Loop over the control points
                for (int iControlPoint = 0; iControlPoint < temp_ControlPoints.Count; iControlPoint++)
                {
                    // Get the control point
                    ControlPoint temp_ControlPoint = temp_ControlPoints[iControlPoint];
                    int temp_NLeafs = temp_ControlPoint.LeafPositions.GetLength(1);

                    // Perform calculations
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
                MmoTherapyBeams[iBeam] = LeafPairsOpenings / NLeafsPairsOpen;

            }

            // Calculate the total mean MLC opening scaling on MUs delivered per beam (inverse beacuse high MU high risk, low opening high risk)
            double MMO_Total = 0;
            double MU_Total = 0;
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                MMO_Total = MMO_Total + MmoTherapyBeams[iBeam] * (1/therapyBeams[iBeam].Meterset.Value);
                MU_Total = MU_Total + therapyBeams[iBeam].Meterset.Value;
            }

            MMO_Total = MMO_Total * MU_Total; // normalize beams per total MUs after weighting per MUs delivered per beam


            // Prepare the list to be returned
            // Pos 0 = number of beams
            retList.Add(NTherapyBeams);

            // Pos 1 ... n = metric values for the single beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                retList.Add(MmoTherapyBeams[iBeam]);
            }

            // Pos n+1 = average of the metric over all the beams
            retList.Add(MMO_Total);

            return retList;


        }


        // Calculate the converted aperture metric according to Götstedt et al  (Med. Phys. 42 (7), July 2015)
        public static List<double> CAM(IEnumerable<Beam> Beams)
        {
            var retList = new List<Double>();

            // Convert the beams to a list and then keep only the therapy fields (exclude setup fields)
            List<Beam> inBeams = Beams.ToList();
            List<Beam> therapyBeams = new List<Beam>();
            foreach (Beam beam in inBeams)
            {
                if (!beam.IsSetupField)
                {
                    therapyBeams.Add(beam);
                }
            }
            int NTherapyBeams = therapyBeams.Count();

            // CAM for each beam
            List<double> CamTherapyBeams = new List<double>();
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                CamTherapyBeams.Add(0);
            }


            // Go over the control points to calculate the CAM
            // Loop over the beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                // Get the control points collection
                ControlPointCollection temp_ControlPoints = null;
                temp_ControlPoints = therapyBeams[iBeam].ControlPoints;

                // Loop over the control points
                for (int iControlPoint = 1; iControlPoint < temp_ControlPoints.Count; iControlPoint++)
                {
                    // Get the control point
                    ControlPoint temp_ControlPoint = temp_ControlPoints[iControlPoint];
                    int temp_NLeafs = temp_ControlPoint.LeafPositions.GetLength(1);

                    // Perform calculations
                    double LeafPairsOpenings = 0;
                    double AreaOpenings = 0;


                    // (1) calculate the openings for this segment
                    for (int iLeaf = 1; iLeaf < temp_NLeafs; iLeaf++)
                    {
                        LeafPairsOpenings = LeafPairsOpenings + Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf] - temp_ControlPoint.LeafPositions[1, iLeaf]); // sum up the openings
                    }

                    // (2) calculate the area for this segment
                    for (int iLeaf = 0; iLeaf < temp_NLeafs; iLeaf++)
                    {
                        AreaOpenings = AreaOpenings + Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf] - temp_ControlPoint.LeafPositions[1, iLeaf]) * Tools.GetLeafWidth(temp_ControlPoint, iLeaf); // how much leaf from bank A is away from leaf bank B *times* leaf width
                    }

                    // go on only if this control point actually has an open segment
                    if (LeafPairsOpenings > 0 && AreaOpenings > 0)
                    {
                        // (3) convert according to the formula proposed by Götstedt et al  (Med. Phys. 42 (7), July 2015), but convert to mm to dm (factor 0.01) to have easy to read CAM values
                        double temp_Fd = 1 - Math.Exp(-0.01 * LeafPairsOpenings);
                        double temp_Fa = 1 - Math.Exp(-0.01 * Math.Sqrt(AreaOpenings));

                        // (4) compute the converted aperture metric for this segment
                        double temp_CAM = 1 - temp_Fd * temp_Fa;

                        // (5) normalize by MUs delivered in this control point and add to the beam CAM
                        CamTherapyBeams[iBeam] = CamTherapyBeams[iBeam] + temp_CAM * Math.Abs(temp_ControlPoints[iControlPoint].MetersetWeight - temp_ControlPoints[iControlPoint - 1].MetersetWeight);
                    }
                    

                }


            }

            // Calculate the total mean CAM scaling on MUs delivered per beam 
            double CAM_Total = 0;
            double MU_Total = 0;
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                CAM_Total = CAM_Total + CamTherapyBeams[iBeam] * therapyBeams[iBeam].Meterset.Value;
                MU_Total = MU_Total + therapyBeams[iBeam].Meterset.Value;
            }

            CAM_Total = CAM_Total / MU_Total; // normalize beams per total MUs after weighting per MUs delivered per beam


            // Prepare the list to be returned
            // Pos 0 = number of beams
            retList.Add(NTherapyBeams);

            // Pos 1 ... n = metric values for the single beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                retList.Add(CamTherapyBeams[iBeam]);
            }

            // Pos n+1 = average of the metric over all the beams
            retList.Add(CAM_Total);

            return retList;

        }


        // Calculate the small aperture score according to Crowe et al 2014 (https://eprints.qut.edu.au/71383/1/crowe_mod_rev.pdf)
        public static List<double> SAS(IEnumerable<Beam> Beams)
        {
            var retList = new List<Double>();

            // Parameter for calculations --> threshold to consider a aperture small:
            double thSA = 5; // in mm

            // Convert the beams to a list and then keep only the therapy fields (exclude setup fields)
            List<Beam> inBeams = Beams.ToList();
            List<Beam> therapyBeams = new List<Beam>();
            foreach (Beam beam in inBeams)
            {
                if (!beam.IsSetupField)
                {
                    therapyBeams.Add(beam);
                }
            }
            int NTherapyBeams = therapyBeams.Count();

            // SAS for each beam
            List<double> SasTherapyBeams = new List<double>();
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                SasTherapyBeams.Add(0);
            }


            // Go over the control points to calculate the mean MLC opening
            // Loop over the beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                // Get the control points collection
                ControlPointCollection temp_ControlPoints = null;
                temp_ControlPoints = therapyBeams[iBeam].ControlPoints;

                int NLeafsPairsOpen = 0;
                int NSmallApertures = 0;

                // Loop over the control points
                for (int iControlPoint = 1; iControlPoint < temp_ControlPoints.Count; iControlPoint++)
                {
                    // Get the control point
                    ControlPoint temp_ControlPoint = temp_ControlPoints[iControlPoint];
                    int temp_NLeafs = temp_ControlPoint.LeafPositions.GetLength(1);

                    // Perform calculations
                    for (int iLeaf = 1; iLeaf < temp_NLeafs; iLeaf++)
                    {
                        double temp_Aperture = Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf] - temp_ControlPoint.LeafPositions[1, iLeaf]);
                        if (temp_Aperture > 0.01) // exclude leaf pairs that are closed
                        {
                            if (temp_Aperture < thSA) { NSmallApertures++; } // keep track of how many small openings we have
                            NLeafsPairsOpen++; // keep track of how many total openings we have
                        }
                    }

                    // Normalize by MU delivered in this control point
                    if (NLeafsPairsOpen > 0)
                    {
                        SasTherapyBeams[iBeam] = SasTherapyBeams[iBeam] + NSmallApertures * (1.0 / NLeafsPairsOpen) * Math.Abs(temp_ControlPoints[iControlPoint].MetersetWeight - temp_ControlPoints[iControlPoint - 1].MetersetWeight);
                    }

                }

            }

            // Calculate the total mean MLC opening scaling on MUs delivered per beam
            double SAS_Total = 0;
            double MU_Total = 0;
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                SAS_Total = SAS_Total + SasTherapyBeams[iBeam] * therapyBeams[iBeam].Meterset.Value;
                MU_Total = MU_Total + therapyBeams[iBeam].Meterset.Value;
            }

            SAS_Total = SAS_Total / MU_Total; // normalize beams per total MUs after weighting per MUs delivered per beam

            // Prepare the list to be returned
            // Pos 0 = number of beams
            retList.Add(NTherapyBeams);

            // Pos 1 ... n = metric values for the single beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                retList.Add(SasTherapyBeams[iBeam]);
            }

            // Pos n+1 = average of the metric over all the beams
            retList.Add(SAS_Total);

            return retList;


        }

        // Calculate the MU/Gy 
        public static List<double> MPG(IEnumerable<Beam> Beams)
        {
            var retList = new List<double>();

            // Convert the beams to a list and then keep only the therapy fields (exclude setup fields)
            List<Beam> inBeams = Beams.ToList();
            List<Beam> therapyBeams = new List<Beam>();
            foreach (Beam beam in inBeams)
            {
                if (!beam.IsSetupField)
                {
                    therapyBeams.Add(beam);
                }
            }
            int NTherapyBeams = therapyBeams.Count();

            // MPG for each beam
            List<double> MpgTherapyBeams = new List<double>();
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                MpgTherapyBeams.Add(0);
            }


            // Go over the control points to calculate the MU per Gy
            // Loop over the beams
            double temp_MUTotal = 0;
            double temp_ATotal = 0;

            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                // Get the MU and Gy values
                double temp_MU = therapyBeams[iBeam].Meterset.Value;
                temp_MUTotal = temp_MUTotal + temp_MU;
                double temp_Gy = therapyBeams[iBeam].Plan.DosePerFraction.Dose;

                // Get the field size
                // Get the control points collection
                ControlPointCollection temp_ControlPoints = null;
                temp_ControlPoints = therapyBeams[iBeam].ControlPoints;

                double temp_A_avg = 0;
                double temp_nA = 0;
                // Loop over the control points
                for (int iControlPoint = 1; iControlPoint < temp_ControlPoints.Count; iControlPoint++)
                {
                    // Get the control point
                    ControlPoint temp_ControlPoint = temp_ControlPoints[iControlPoint];

                    // Perform calculations
                    int temp_NLeafs = temp_ControlPoint.LeafPositions.GetLength(1);

                    // Calculate area of the opening for this control point
                    double temp_A = 0;
                    for (int iLeaf = 0; iLeaf < temp_NLeafs; iLeaf++)
                    {
                        temp_A = temp_A + Math.Abs(temp_ControlPoint.LeafPositions[0, iLeaf] - temp_ControlPoint.LeafPositions[1, iLeaf]) * Tools.GetLeafWidth(temp_ControlPoint, iLeaf); // how much leaf from bank A is away from leaf bank B *times* leaf width
                    }

                    temp_A_avg = temp_A_avg + temp_A;
                    temp_nA = temp_nA + 1;
                }

                // average area of this arc
                temp_A_avg = temp_A_avg / temp_nA;
                temp_ATotal = temp_ATotal + temp_A_avg; // keep track for total

                // calculate the metric
                MpgTherapyBeams[iBeam] = (temp_MU / temp_Gy) / temp_A_avg; // normalize per field size

                //MpgTherapyBeams[iBeam] = therapyBeams[iBeam].MetersetPerGy; // or use the one already implemented by Varian

            }

            // Calculate the total MPG
            double MPG_Total = (temp_MUTotal / therapyBeams[0].Plan.TotalDose.Dose) / (temp_ATotal / NTherapyBeams);

            // Prepare the list to be returned
            // Pos 0 = number of beams
            retList.Add(NTherapyBeams);

            // Pos 1 ... n = metric values for the single beams
            for (int iBeam = 0; iBeam < NTherapyBeams; iBeam++)
            {
                retList.Add(MpgTherapyBeams[iBeam]);
            }

            // Pos n+1 = average of the metric over all the beams
            retList.Add(MPG_Total);

            return retList;
        }

    }
}
