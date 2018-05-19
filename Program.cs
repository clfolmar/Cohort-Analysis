using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Cohort_Analysis
{
    class Program
    {
        // Data Files
        public static string inPatientFilePath = "inpatient.txt",
                outPatientFilePath = "outpatient.txt",
                carrierFilePath = "carrier.txt",
                prescriptionFilePath = "prescription.txt",
                beneficiaryFilePath = "beneficiary.txt",
                lovastatinFilePath = "lovastatin.txt",

                // Headers
                dgns = "DGNS",
                claimFromDate = "CLM_FROM_DT",
                birthDate = "BENE_BIRTH_DT",
                scriptServiceDate = "SRVC_DT",
                scriptProductId = "PROD_SRVC_ID",

                // Constants
                diabetesDgnsCode = "250",
                diabetesClaimYear = "2009",
                flatDataFormat = "yyyyMMdd";

        // Indexes to retrieve
        public static int inPatientClaimDateIndex = 0,
            outPatientClaimDateIndex = 0,
            carrierClaimDateIndex = 0,
            prescriptionServiceDateIndex = 0,
            prescriptionProductIdIndex = 0,
            dobIndex = 0;

        // data structures to hold indexes for headers
        public static List<int> inPatientDGNS = new List<int>();
        public static List<int> outPatientDGNS = new List<int>();
        public static List<int> carrierDGNS = new List<int>();
        // data structure to hold Cohort One Qualifying Patients
        public static Dictionary<string, DateTime> cohortOneQualifyingPatients = new Dictionary<string, DateTime>();
        public static Dictionary<string, DateTime> cohortTwoQualifyingPatients = new Dictionary<string, DateTime>();
        public static Dictionary<string, DateTime> cohortThreeQualifyingPatients = new Dictionary<string, DateTime>();
        static void Main(string[] args)
        {
            // Inpatient
            string[,] inPatientCollection = ConfigurePatientData(inPatientFilePath, dgns, inPatientDGNS, claimFromDate, out inPatientClaimDateIndex);

            // Outpatient
            string[,] outPatientCollection = ConfigurePatientData(outPatientFilePath, dgns, outPatientDGNS, claimFromDate, out outPatientClaimDateIndex);

            // Carrier
            string[,] carrierCollection = ConfigurePatientData(carrierFilePath, dgns, carrierDGNS, claimFromDate, out carrierClaimDateIndex);

            // Beneficiary
            string[,] beneficiaryCollection = ConfigureBeneficiaryData(beneficiaryFilePath, birthDate, out dobIndex);

            // Prescription
            string[,] prescriptionCollection = ConfigurePrescriptionData(prescriptionFilePath, scriptServiceDate, out prescriptionServiceDateIndex, scriptProductId, out prescriptionProductIdIndex);

            // Lovastatin
            List<string> lovastatinCollection = ConfigureLovastatinData(lovastatinFilePath);

            // Cohort Ones
            CohortOne(inPatientCollection, inPatientDGNS, inPatientClaimDateIndex);
            CohortOne(outPatientCollection, outPatientDGNS, outPatientClaimDateIndex);
            CohortOne(carrierCollection, carrierDGNS, carrierClaimDateIndex);

            Console.WriteLine("Node 1 Count = " + cohortOneQualifyingPatients.Count);

            // Cohort Two
            CohortTwo(prescriptionCollection, lovastatinCollection, prescriptionServiceDateIndex, prescriptionProductIdIndex);

            Console.WriteLine("Node 2 Count = " + cohortTwoQualifyingPatients.Count);

            // Cohort Three
            CohortThree(beneficiaryCollection, dobIndex);

            Console.WriteLine("Node 3 Count = " + cohortThreeQualifyingPatients.Count);

            // For User readability
            Console.ReadLine();
        }

        /// <summary>
        /// Select	patients with a diagnosis of diabetes (diagnosis code ‘250*’)	in	the	year 2009.	The	
        //  first diabetes diagnosis date in 2009 for each patient	is referenced as the “Diabetes Index Date”. 
        //  Any diagnosis code from any file(Inpatient, Outpatient, Carrier) starting with ‘250' can be considered a match.
        /// </summary>
        /// <param name="patientData"></param>
        /// <param name="diagnosisIndexes"></param>
        /// <param name="claimDateIndex"></param>
        /// <param name="cohortOneQualifyingPatients"></param>
        private static void CohortOne(string[,] patientData, List<int> diagnosisIndexes, int claimDateIndex)
        {
            // number of records
            int recordCount = patientData.GetUpperBound(0) - patientData.GetLowerBound(0) + 1;

            // iterate through records
            for (int i = 0; i < recordCount; i++)
            {
                // for each diagnosis column
                foreach (int index in diagnosisIndexes)
                {
                    // if the diagnosis code starts with 250 and the date starts with 2009
                    if (patientData[i, index] != null &&
                        patientData[i, index].StartsWith(diabetesDgnsCode) &&
                        patientData[i, claimDateIndex] != null &&
                        patientData[i, claimDateIndex].Contains(diabetesClaimYear))
                    {
                        DateTime recordDate = DateTime.ParseExact(patientData[i, claimDateIndex], flatDataFormat, CultureInfo.CurrentCulture);
                        string patientId = patientData[i, 0];

                        // check if patient has already been added to the Dictionary of qualifying patients
                        if (!cohortOneQualifyingPatients.TryGetValue(patientId, out DateTime storedDID))
                        {
                            // if not then add to the list of patients qualifying Cohort One
                            // add the qualifying patient to the Dictionary of patients meeting Cohort one criteria
                            cohortOneQualifyingPatients.Add(patientId, recordDate);
                        }
                        else
                        {
                            // we know it's an accounted for qualifying patient, thus ensure we have the correct Diabetes Index Date (earliest date serviced)
                            // check if the record date is earlier than the saved DID (earliest date serviced)
                            if (recordDate < storedDID)
                            {
                                // if so update the date
                                storedDID = recordDate;
                            }

                        }
                    }
                }
                i++;
            }

        }

        /// <summary>
        /// Filter to patients with a prescription of Lovastatin within one year following their Diabetes Index Date (D.I.D.).
        /// </summary>
        /// <param name="prescriptionCollection"></param>
        /// <param name="lovastatinData"></param>
        /// <param name="serviceDateIndex"></param>
        /// <param name="productIdIndex"></param>
        /// <param name="cohortOneQualifiedPatients"></param>
        /// <returns></returns>
        private static void CohortTwo(string[,] prescriptionCollection, List<string> lovastatinData, int serviceDateIndex, int productIdIndex)
        {
            // number of records
            int recordCount = prescriptionCollection.GetUpperBound(0) - prescriptionCollection.GetLowerBound(0) + 1;

            // iterate through each line of prescription collection data
            for (int i = 0; i < recordCount; i++)
            {
                string patientId = prescriptionCollection[i, 0];
                // ensure patientId != null,
                if (patientId != null &&
                // ensure the current record is for a Cohort One qualified patient, and 
                cohortOneQualifyingPatients.TryGetValue(patientId, out DateTime storedDID) &&
                // that the patient has NOT already been qualified for Cohort Two
                !cohortTwoQualifyingPatients.TryGetValue(patientId, out DateTime storedDIDValue))
                {
                    DateTime convertedServiceDate = DateTime.ParseExact(prescriptionCollection[i, serviceDateIndex], "yyyyMMdd", CultureInfo.CurrentCulture);

                    // for each script product ID in the lovastatin data collection
                    foreach (string lovastatinScript in lovastatinData)
                    {
                        // if script field from prescription Collection matches lovastatin id && serviceDate field is *within one year* of the diabetes index date
                        if (prescriptionCollection[i, productIdIndex].Equals(lovastatinScript) && convertedServiceDate < storedDID.AddYears(1))
                        {
                            // patient qualifies for Cohort Two, thus add them to our collection
                            cohortTwoQualifyingPatients.Add(patientId, storedDID);
                            break;
                        }
                    }
                }
                // increment iterator to advance to next line of our prescription collection
                i++;
            }
        }

        /// <summary>
        /// Filter to patients who are greater than or equal to 65 years of age at the time of their Diabetes Index Date (D.I.D.)
        /// </summary>
        /// <param name="beneficiaryCollection"></param>
        /// <param name="dobIndex"></param>
        /// <param name="CohortTwoQualifiedPatients"></param>
        /// <returns></returns>
        private static void CohortThree(string[,] beneficiaryCollection, int dobIndex)
        {
            int recordCount = beneficiaryCollection.GetUpperBound(0) - beneficiaryCollection.GetLowerBound(0) + 1;

            for (int i = 0; i < recordCount; i++)
            {
                string patientId = beneficiaryCollection[i, 0];
                // ensure the patientId != null, 
                if (patientId != null &&
                    // the record is for a qualifying patient
                    cohortTwoQualifyingPatients.TryGetValue(patientId, out DateTime storedDID) &&
                    // and that the patient has NOT already been qualified for Cohort Three
                    !cohortThreeQualifyingPatients.TryGetValue(patientId, out DateTime storedDIDValue))
                {
                    // convert the dob to a DateTime object we can work with
                    DateTime convertedDob = DateTime.ParseExact(beneficiaryCollection[i, dobIndex], "yyyyMMdd", CultureInfo.CurrentCulture);

                    // calculate the date of the patient's 65th birthday
                    DateTime convertedSixtyFiveYearsOfAgeDate = convertedDob.AddYears(65);

                    // check if the qualified patient's Diabetes Index Date is after or on the patient's 65 Birthday
                    if (storedDID >= convertedSixtyFiveYearsOfAgeDate)
                    {
                        // if so, add the patient to our collection of Cohort Three qualified patients 
                        cohortThreeQualifyingPatients.Add(patientId, storedDID);
                    }
                }
                i++;
            }
        }

        /// <summary>
        /// Helper Method to:
        /// -populate a data collection from a text data file that only has one column of data per row
        /// </summary>
        /// <param name="lovastatinFilePath"></param>
        /// <returns></returns>
        private static List<string> ConfigureLovastatinData(string lovastatinFilePath)
        {
            List<string> lovastatinCollection = new List<string>();
            string line;
            // read in the data file
            StreamReader file = new StreamReader(lovastatinFilePath);

            // while we have a line of data to read
            while ((line = file.ReadLine()) != null)
            {
                // add the data to the collection
                lovastatinCollection.Add(line);
            }
            //return the collection
            return lovastatinCollection;
        }

        /// <summary>
        /// Helper Method to:
        /// -populate a data collection from a text data file
        /// -find the indexes for a column within the data file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchTerm"></param>
        /// <param name="searchTermIndex"></param>
        private static string[,] ConfigureBeneficiaryData(string path, string searchTerm, out int searchTermIndex)
        {

            int rowCounter = 0, columnCounter = 0;
            string line;

            searchTermIndex = 0;

            try
            {
                // read in file
                StreamReader file = new StreamReader(path);

                while ((line = file.ReadLine()) != null)
                {
                    // if header row of data, we need to acquire index(es) for the search term(s)
                    if (rowCounter == 0)
                    {
                        // split columns by ','
                        foreach (string col in line.Trim().Split(','))
                        {
                            // if header row of data, we need to acquire index(es) for the search term(s)
                            if (rowCounter == 0)
                            {
                                // if the column contains the first search term we're searching for
                                if (col.Trim().Contains(searchTerm))
                                {
                                    // record that index
                                    searchTermIndex = columnCounter;
                                }
                            }
                            // increment the column
                            columnCounter++;
                        }
                    }
                    // increment the row
                    rowCounter++;
                }

                // create our 2d string array to hold our columns and rows of data
                string[,] collection = new string[rowCounter, columnCounter];
                // close the file stream
                file.Close();
                // reset the counters to re-iterate through the file
                rowCounter = 0;
                // reset the stream reader
                file = new StreamReader(path);
                while ((line = file.ReadLine()) != null)
                {
                    // reset the column counter everytime we start evaluating a new line
                    columnCounter = 0;
                    foreach (string col in line.Trim().Split(','))
                    {
                        // if it's a row of data other than the header row
                        if (rowCounter != 0)
                        {
                            // populate collection
                            collection[rowCounter, columnCounter] = col.Trim();
                        }
                        // increment the column
                        columnCounter++;
                    }
                    // increment the row
                    rowCounter++;
                }
                // close the file stream
                file.Close();
                //return the populated collection of data
                return collection;

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Message: " + e.Message);
                Console.WriteLine("Inner Exception  Message: " + e.InnerException.Message);
                Console.ReadLine();
            }

            // if we reach here we have an issue
            return null;
        }

        /// <summary>
        /// Helper Method to:
        /// -populate a data collection from a text data file
        /// -find the index for each search term representing a column header in the data file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchTerm"></param>
        /// <param name="searchTermIndex"></param>
        /// <param name="searchTermTwo"></param>
        /// <param name="searchTermTwoIndex"></param>
        private static string[,] ConfigurePrescriptionData(string path, string searchTerm, out int searchTermIndex, string searchTermTwo, out int searchTermTwoIndex)
        {
            int rowCounter = 0, columnCounter = 0;
            string line;

            searchTermIndex = 0;
            searchTermTwoIndex = 0;

            try
            {
                // read in file
                StreamReader file = new StreamReader(path);

                while ((line = file.ReadLine()) != null)
                {
                    // if header row of data, we need to acquire index(es) for the search term(s)
                    if (rowCounter == 0)
                    {
                        // split columns by ','
                        foreach (string col in line.Trim().Split(','))
                        {
                            // if header row of data, we need to acquire index(es) for the search term(s)
                            if (rowCounter == 0)
                            {
                                // if the column contains the first search term we're searching for
                                if (col.Trim().Contains(searchTerm))
                                {
                                    // record that index
                                    searchTermIndex = columnCounter;
                                }
                                // if the column contains our second search term
                                else if (col.Trim().Contains(searchTermTwo))
                                {
                                    // record that index
                                    searchTermTwoIndex = columnCounter;
                                }
                            }
                            // increment the column
                            columnCounter++;
                        }
                    }

                    // increment the row
                    rowCounter++;
                }

                // create our 2d string array to hold our columns and rows of data
                string[,] collection = new string[rowCounter, columnCounter];
                // close the file stream
                file.Close();
                // reset the counters to re-iterate through the file
                rowCounter = 0;
                // reset the stream reader
                file = new StreamReader(path);
                while ((line = file.ReadLine()) != null)
                {
                    // reset the column counter everytime we start evaluating a new line
                    columnCounter = 0;
                    foreach (string col in line.Trim().Split(','))
                    {
                        // if header row
                        if (rowCounter == 0)
                        {
                            //skip
                            break;
                        }
                        else // it's a row of data
                        {
                            // populate collection utilizing rowCounter - 1 to fill first row of the 2d Array with data
                            // instead of the header column from the data file
                            collection[rowCounter - 1, columnCounter] = col.Trim();
                            // increment the column counter
                            columnCounter++;
                        }
                    }
                    // increment row counter
                    rowCounter++;
                }
                // close the file stream
                file.Close();
                //return the populated collection of data
                return collection;

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Message: " + e.Message);
                Console.WriteLine("Inner Exception  Message: " + e.InnerException.Message);
                Console.ReadLine();
            }

            // if we reach here we have an issue
            return null;
        }

        /// <summary>
        /// Helper Method to:
        /// -populate a data collection from a text data file
        /// -find the multiple indexes for the column headers matching first search term and 
        /// single index for the second search term within the data file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchTerm"></param>
        /// <param name="searchTermIndex"></param>
        /// <param name="searchTermTwo"></param>
        /// <param name="searchTermTwoIndex"></param>
        private static string[,] ConfigurePatientData(string path, string searchTerm, List<int> searchTermIndex, string searchTermTwo, out int searchTermTwoIndex)
        {
            int rowCounter = 0, columnCounter = 0;
            string line;

            searchTermTwoIndex = 0;

            try
            {
                // read in file
                StreamReader file = new StreamReader(path);

                while ((line = file.ReadLine()) != null)
                {
                    // if header row of data, we need to acquire index(es) for the search term(s)
                    if (rowCounter == 0)
                    {
                        // split columns by ','
                        foreach (string col in line.Trim().Split(','))
                        {
                            // if the column contains the term we're searching for
                            if (col.Trim().Contains(searchTerm))
                            {
                                // add it to our list of indexes for that search term
                                searchTermIndex.Add(columnCounter);
                            }
                            // if the column contains our second search term
                            else if (col.Trim().Contains(searchTermTwo))
                            {
                                // record that index
                                searchTermTwoIndex = columnCounter;
                            }
                            // increment the column
                            columnCounter++;
                        }
                    }
                    // increment the row
                    rowCounter++;
                }

                // create our 2d string array to hold our columns and rows of data
                string[,] collection = new string[rowCounter, columnCounter];
                // close the file stream
                file.Close();
                // reset the counters to re-iterate through the file
                rowCounter = 0;
                // reset the stream reader
                file = new StreamReader(path);
                while ((line = file.ReadLine()) != null)
                {
                    columnCounter = 0;
                    foreach (string col in line.Trim().Split(','))
                    {
                        // if header row
                        if (rowCounter == 0)
                        {
                            // skip
                            break;
                        }
                        else // it's a row of data
                        {
                            // populate collection utilizing rowCounter - 1 to fill first row of the 2d Array with data
                            // instead of the header column from the data file
                            collection[rowCounter - 1, columnCounter] = col.Trim();
                            // increment the column counter
                            columnCounter++;
                        }
                    }
                    // increment row counter
                    rowCounter++;
                }
                // close the file stream
                file.Close();
                // return the populated collection of data
                return collection;

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Message: " + e.Message);
                Console.WriteLine("Inner Exception  Message: " + e.InnerException.Message);
                Console.ReadLine();
            }

            // if we reach here we have an issue
            return null;
        }
    }
}
