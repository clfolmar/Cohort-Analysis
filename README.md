# Cohort-Analysis
C# Console Application for programmatic Cohort Analysis on flat data files

From the given 3072 patient data sample, please determine the counts of patients eligible for each cohort node criteria below.  

To clarify, only the patients who meet the Cohort 1 criteria should be considered for Cohort 2, and only patients who meet the Cohort 2 criteria should be considered for Cohort 3.

For	Outpatient, Inpatient, and Carrier please use the “CLM_FROM_DT” for the date variable. For prescriptions, use “SRVC_DT”

<h2>Cohort One:</h2>
Select patients with a diagnosis of diabetes (diagnosis code ‘250*’) in the year 2009. The first diabetes diagnosis date in 2009 for each patient is referenced as the “Diabetes Index Date”.  Any diagnosis code from any file (Inpatient, Outpatient, Carrier) starting with ‘250’ can be considered a match.
      
<h2>Cohort Two:</h2>
Filter to patients	with a prescription of Lovastatin within one year following their diabetes index date. Please use the included lovastatin.txt NDC codes to find a	match.
      
<h2>Cohort Three:</h2>
Filter to patients who are greater than or equal to 65 years old at the time of their diabetes index date.

<h2>Results:</h2>

<b>Node 1 (Diabetes):    1337 Patients

Node 2 (Lovastatin):  135 Patients

Node 3 (Age):         57 Patients</b>
