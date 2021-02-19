using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Globalization;

namespace ph_scraper_v2
{
    class Symptom
    {
        public string name;
        public string description;
        public int probability;
        public string discomfort;
        public bool patientComplains;
        public string hazard;
        public string collapseStart;
        public string collapseEnd;
        public string deathStart;
        public string deathEnd;
        public string collapseSymptom;
        public string mobility;
        public bool isMainSymptom;
        public List<string> examinations;
        public List<string> treatments;
        public int shameLevel;

        public Symptom()
        {
            name = description = discomfort = hazard = collapseSymptom = mobility = collapseStart = collapseEnd = deathStart = deathEnd = "N/A";
            probability = shameLevel = -1;
            patientComplains = isMainSymptom = false;
            
            examinations = new List<string>();
            treatments = new List<string>();
        }
    }

    class Diagnosis
    {
        public string name;
        public string description;
        public string occurrence;
        public List<Symptom> symptoms;
        public string department;
        public int payment;

        public Diagnosis()
        {
            name = description = occurrence = department = "N/A";
            payment = -1;

            symptoms = new List<Symptom>();
        }
    }

    class Department
    {
        public string name;
        public int totalDiagnoses;
        public int totalSeriousDiagnoses;
        public int totalFatalDiagnoses;
        public Dictionary<string, int> examUsage; 
        public Dictionary<string, int> urgentExamUsage;

        public Department(string name)
        {
            this.name = name;
            totalDiagnoses = totalSeriousDiagnoses = totalFatalDiagnoses = 0;
            
            examUsage = new Dictionary<string, int>();
            urgentExamUsage = new Dictionary<string, int>();
        }
    }

    class Auxilliary
    {
        public string name;
        public string description;
        public string rooms;
        public string equipment;

        public Auxilliary()
        {
            name = description = rooms = equipment = "N/A";
        }
    }

    class Scraper
    {
        // Contains a reference to all diagnoses currently being processed.
        static List<Diagnosis> diagnoses;
        // Contains all the symptoms in the game, accessed by symptom ID.
        static Dictionary<string, Symptom> symptoms;
        // Contains all localization strings, accessed by a localization ID.
        static Dictionary<string, string> localization;
        static Dictionary<string, string> examPairs;
        static Dictionary<string, string> treatmentPairs;
        static List<string> nameList;
        static List<string> descList;
        static void Scrape()
        {
            // For readability, these store the basic file paths which are passed to the various methods.
            string baseGamePath = @".\Input\Base-Game";
            string baseGameDiagnosisPath = baseGamePath + "\\Diagnoses";
            string baseGameLocalizationPath = baseGamePath + "\\Localization";
            string baseGameSymptomPath = baseGamePath + "\\Symptoms";
            string baseGameOutputPath = @".\Output\Base-Game";
            string normalizationPath = @".\Input\Normalization";
            string auxOutputPath = @".\Output\Auxilliary";

            string modGamePath = @".\Input\Modded";
            string modGameOutputPath = @".\Output\Mod-Diagnoses";
            string crpPath = @"\Community-Resource-Pack";
            string entPath = @"\ENT";
            string gynPath = @"\Gynecology";
            string oncPath = @"\Oncology";
            string plsPath = @"\Plastic-Surgery";
            string sxhPath = @"\Sexual-Health";
            string urnPath = @"\Urology-Nephrology";

            // Set this flag to false if you don't want to process modded departments.
            bool processModdedDepartments = true;

            // Process the symptom, localization, and diagnoses files in their respective directories. The symptom files should be processed before the diagnosis files.
            processAllFilesInDirectory(baseGameSymptomPath, "symptom");
            processAllFilesInDirectory(baseGameLocalizationPath, "localization");
            processAllFilesInDirectory(baseGameDiagnosisPath, "diagnosis");

            // Generates "grammatical pairs" which help to better format the output of the guide.
            generateGrammaticalPairs(normalizationPath, "exams", false);
            generateGrammaticalPairs(normalizationPath, "treatments", false);

            // Process all of the departments in the base game individually.
            Department cardiology = new Department("Cardiology");
            generateOutputFile(baseGameOutputPath, cardiology, "DPT_CARDIOLOGY");

            Department emergency = new Department("Emergency");
            generateOutputFile(baseGameOutputPath, emergency, "DPT_EMERGENCY");

            Department infectious = new Department("Infectious-Diseases");
            generateOutputFile(baseGameOutputPath, infectious, "DPT_INFECTIOUS_DISEASES_DEPARTMENT");

            Department intern = new Department("Internal-Medicine");
            generateOutputFile(baseGameOutputPath, intern, "DPT_INTERNAL_MEDICINE_DEPARTMENT");

            Department neurology = new Department("Neurology");
            generateOutputFile(baseGameOutputPath, neurology, "DPT_NEUROLOGY");

            Department orthopedics = new Department("Orthopedics");
            generateOutputFile(baseGameOutputPath, orthopedics, "DPT_ORTHOPAEDICS_AND_TRAUMATOLOGY");

            Department surgery = new Department("General-Surgery");
            generateOutputFile(baseGameOutputPath, surgery, "DPT_GENERAL_SURGERY_DEPARTMENT");

            Department trauma = new Department("Traumatology");
            generateOutputFile(baseGameOutputPath, trauma, "DPT_TRAUMATOLOGY_DEPARTMENT");

            // Symptoms, Examinations, Treatments
            generateAuxilliaryOutputFile(auxOutputPath, "symptoms");
            generateAuxilliaryOutputFile(auxOutputPath, "exams");
            generateAuxilliaryOutputFile(auxOutputPath, "treatments");

            // If the flag is set to true, this block will process all modded department files currently available.
            if (processModdedDepartments)
            {
                // This gets called for the Community Resource Pack since it only contains symptoms and localization files that we actually care about.
                processAllModdedFilesForDept(modGamePath + crpPath, @"\Symptoms", @"\Localization", @"\Diagnoses");

                processModdedDepartment(modGamePath + entPath, modGameOutputPath, "Ears-Nose-and-Throat", "DPT_OTORHINOLARYNGOLOGY");
                processModdedDepartment(modGamePath + gynPath, modGameOutputPath, "Gynecology", "DPT_GYNECOLOGIE_DEPARTMENT");
                processModdedDepartment(modGamePath + oncPath, modGameOutputPath, "Oncology", "DPT_ONCOLOGY");
                processModdedDepartment(modGamePath + oncPath, modGameOutputPath, "Neurology-Oncology", "DPT_NEUROLOGY");
                processModdedDepartment(modGamePath + plsPath, modGameOutputPath, "Plastic-Surgery", "DPT_PLASTICS");
                processModdedDepartment(modGamePath + sxhPath, modGameOutputPath, "Sexual-Health", "DPT_SH");
                processModdedDepartment(modGamePath + urnPath, modGameOutputPath, "Urology-Nephrology", "DPT_URKN");
            }
        }

        // Generate an auxilliary output file based on the selection. This will generate a file either on symptoms, examinations, or treatments.
        static void generateAuxilliaryOutputFile(string outputPath, string selection)
        {
            switch (selection)
            {
                case "symptoms":
                    generateSymptomsFile(outputPath + @"\Symptoms.txt");
                    break;
                case "exams":
                    generateExamsOrTreatments(outputPath + @"\Exams.txt", "EXM_");
                    break;
                case "treatments":
                    generateExamsOrTreatments(outputPath + @"\Treatments.txt", "TRT_");
                    break;
            }
        }

        // This method will generate the acutal symptom output file. Of the auxilliary output files, this one is the most in-depth, hence why it gets its own
        // method.
        static void generateSymptomsFile(string file)
        {
            // Delete the file if it exists.
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            // File stream and pointer for the main output file.
            FileStream ofs = File.Create(file);
            StreamWriter ofp = new StreamWriter(ofs);

            List<Symptom> symptomList = new List<Symptom>();

            // Generate a list of symptoms. This is used for alphabetization purposes.
            foreach (KeyValuePair<string, Symptom> kvp in symptoms)
            {
                Symptom s = kvp.Value;
                s.name = localization[s.name];

                symptomList.Add(s);
            }

            // Alphabetize the list.
            symptomList.Sort(delegate(Symptom x, Symptom y)
            {
                if (x.name == null && y.name == null) 
                    return 0;
                else if (x.name == null) 
                    return -1;
                else if (y.name == null) 
                    return -1;
                else 
                    return x.name.CompareTo(y.name);
            });

            ofp.WriteLine("Total Symptoms: " + symptomList.Count);
            ofp.WriteLine("");

            // Generate the output file.
            foreach (Symptom s in symptomList)
            {
                ofp.WriteLine("## {0}", normalize(s.name));
                ofp.WriteLine("");
                ofp.WriteLine("{0}", localization[s.description]);
                ofp.WriteLine("");

                // Normalize the mobility string.
                if (s.mobility.Equals("IMOBILE"))
                {
                    s.mobility = "Immobile";
                }
                else
                {
                    s.mobility = "Mobile";
                }

                // Various helpful information-y bits.
                ofp.WriteLine("__Discomfort Level__: {0} | __Patient Complains__: {1} | __Shame Level__: {2}", s.discomfort, s.patientComplains, s.shameLevel);
                ofp.WriteLine("__Mobility__: {0} | __Hazard__: {1}", s.mobility, s.hazard);

                // Record the collapse symptom.
                if (!s.collapseSymptom.Equals("N/A"))
                {
                    ofp.WriteLine("Can lead to a __collapse__. | Collapse Symptom: __{0}__ | Timeframe: __{1} to {2}__ hours.", localization[s.collapseSymptom], s.collapseStart, s.collapseEnd);
                }

                // Record if the symptom is potentially fatal.
                if (!s.deathEnd.Equals("N/A"))
                {
                    ofp.WriteLine("Can lead to __death__. | Timeframe: __{0} to {1}__ hours.", s.deathStart, s.deathEnd);
                }
                
                // Note if this symptom is the main symptom for a diagnosis.
                if (s.isMainSymptom)
                {
                    ofp.WriteLine("This is the main symptom for a diagnosis.");
                }

                ofp.WriteLine("");

                // List the examinations that can uncover this symptom.
                ofp.WriteLine("__Examinations:__");

                foreach (string e in s.examinations)
                {
                    ofp.WriteLine("+ {0}", localization[e]);
                }

                ofp.WriteLine("");

                // List the treatments used to suppress / treat this symptom.
                ofp.WriteLine("__Treatments:__");

                foreach (string t in s.treatments)
                {
                    ofp.WriteLine("+ {0}", localization[t]);
                }

                ofp.WriteLine("");
            }

            ofp.Flush();
            ofp.Close();
        }

        // Creates the examinations and treatments auxiliary output files.
        static void generateExamsOrTreatments(string file, string selection)
        {
            // Delete the file if it exists.
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            // File stream and pointer for the main output file.
            FileStream ofs = File.Create(file);
            StreamWriter ofp = new StreamWriter(ofs);

            List<Auxilliary> auxList = new List<Auxilliary>();

            // Set up the output list for alphabetization purposes.
            foreach (string s in nameList)
            {
                Auxilliary aux = new Auxilliary();
                int index = descList.FindIndex(x => x.Contains(s));

                // Find and format the description.
                if (s.Contains(selection))
                {
                    aux.name = s;

                    // If there is no description, just set the name as the description.
                    if (index == -1)
                    {
                        aux.description = s;
                    }
                    // Format the description and place room and equipment requirements on separate lines.
                    else
                    {
                        aux.description = localization[descList[index]];

                        int roomIndex = aux.description.IndexOf("\\n\\nRequired room");
                        int equipIndex = aux.description.IndexOf("\\n\\nRequired equipment");

                        if (roomIndex != -1)
                        {
                            aux.rooms = aux.description.Substring(roomIndex);
                        }

                        if (equipIndex != -1)
                        {
                            aux.equipment = aux.description.Substring(equipIndex);
                        }
                    }

                    auxList.Add(aux);
                }
            }

            // Alphabetize the list.
            auxList.Sort(delegate(Auxilliary x, Auxilliary y)
            {
                string xL = localization[x.name];
                string yL = localization[y.name];

                if (x.name == null && y.name == null) 
                    return 0;
                else if (x.name == null) 
                    return -1;
                else if (y.name == null) 
                    return -1;
                else 
                    return xL.CompareTo(yL);
            });

            string selectionString = (selection.Equals("EXM_")) ? "Exams" : "Treatments";

            ofp.WriteLine("Total " + selectionString + ": " + auxList.Count);
            ofp.WriteLine("");

            // Run through the auxiliary list and print out all the data.
            foreach (Auxilliary aux in auxList)
            {
                ofp.WriteLine("### {0}", localization[aux.name]);

                // Remove previous lines from the description and 
                aux.description = aux.description.Replace(aux.rooms, "");
                aux.description = aux.description.Replace(aux.equipment, "");
                aux.rooms = aux.rooms.Replace(aux.equipment, "");

                ofp.WriteLine("{0}", aux.description);

                if (!aux.rooms.Equals("N/A"))
                {
                    ofp.WriteLine("+ {0}", aux.rooms.Replace("\\n", ""));
                }

                if (!aux.equipment.Equals("N/A"))
                {
                    ofp.WriteLine("+ {0}", aux.equipment.Replace("\\n", ""));
                }

                ofp.WriteLine("");
            }

            ofp.Flush();
            ofp.Close();
        }

        static void processModdedDepartment(string modDeptPath, string modGameOutputPath, string deptName, string deptID)
        {
            string normalizationPath = @".\Input\Normalization";
            string diagnosesPath = @"\Diagnoses";
            string localizationPath = @"\Localization";
            string symptomsPath = @"\Symptoms";

            // Clear collections in preparation for processing modded departments.
            clearCollections();

            processAllModdedFilesForDept(modDeptPath, symptomsPath, localizationPath, diagnosesPath);
            appendGrammaticalPairs(normalizationPath);

            Department newDept = new Department(deptName);
            generateOutputFile(modGameOutputPath, newDept, deptID);
        }

        // Processes all files within a modded department's folder.
        static void processAllModdedFilesForDept(string moddedFilePath, string modSymptomPath, string modLocalizationPath, string modDiagnosesPath)
        {
            processAllFilesInDirectory(moddedFilePath + modSymptomPath, "symptom");
            processAllFilesInDirectory(moddedFilePath + modLocalizationPath, "localization");
            processAllFilesInDirectory(moddedFilePath + modDiagnosesPath, "diagnosis");
        }

        // Appends modded grammatical pairs to the normalization files.
        static void appendGrammaticalPairs(string normalizationPath)
        {
            generateGrammaticalPairs(normalizationPath, "exams", true);
            generateGrammaticalPairs(normalizationPath, "treatments", true);
        }

        // Clears the various lists and dictionaries in preparation for modded departments.
        static void clearCollections()
        {
            diagnoses.Clear();
            //symptoms.Clear();
            //localization.Clear();
        }

        // Creates grammatical pairs to create more pleasing output files. The base game files tend to not spell things correctly, fail to use conventional title case,
        // or name exams / treatments in a way not conducive to the overall aesthetic of the guide. This method aims to fix that with manually-created normalization files.
        static void generateGrammaticalPairs(string path, string selection, bool appendToFile)
        {
            string game = path;
            string normalized = path;

            // Select the correct path based on the desired normalization type.
            switch (selection)
            {
                case "exams":
                    game += "\\game-exams.txt";
                    normalized += "\\normalized-exams.txt";
                    break;
                case "treatments":
                    game += "\\game-treatments.txt";
                    normalized += "\\normalized-treatments.txt";
                    break;
            }

            if (appendToFile)
            {
                appendGameText(game, selection);
            }
            else
            {
                generateGameText(game, selection);
            }

            string[] gameText = System.IO.File.ReadAllLines(game);
            string[] normalText = System.IO.File.ReadAllLines(normalized);

            // Loop through both files simultaneously, adding the key value pairs either to the exam or treatment pair dictionary.
            for (int i = 0; i < gameText.Length; i++)
            {
                switch (selection)
                {
                    case "exams":
                        examPairs.TryAdd(gameText[i], normalText[i]);
                        break;
                    case "treatments":
                        treatmentPairs.TryAdd(gameText[i], normalText[i]);
                        break;
                }
            }
        }

        static void appendGameText(string file, string selection)
        {
            FileStream ofs = File.Create(file);
            StreamWriter ofp = new StreamWriter(ofs);

            foreach (KeyValuePair<string, string> kvp in localization)
            {
                string key = kvp.Key;
                string value = kvp.Value;

                if (!key.Contains("_DESCRIPTION") && !key.Contains("_DESC"))
                {
                    if (key.Contains("EXM_") && selection.Equals("exams"))
                    {
                        ofp.WriteLine(value);
                    }
                    else if (key.Contains("TRT_") && selection.Equals("treatments"))
                    {
                        ofp.WriteLine(value);
                    }
                    
                }
            }

            ofp.Flush();
            ofp.Close();
        }

        // Automatically updates the game-exams and game-treatments files. The normalized files must be updated manually.
        static void generateGameText(string file, string selection)
        {
            // Copy the file in case something horrible happens in the future.
            string destExams = @".\Input\Normalization\previous-game-exams.txt";
            string destTreatments = @".\Input\Normalization\previous-game-treatments.txt";

            if (File.Exists(destExams) && selection.Equals("exams"))
            {
                File.Delete(destExams);
                File.Copy(file, destExams);
            }

            if (File.Exists(destTreatments) && selection.Equals("treatments"))
            {
                File.Delete(destTreatments);
                File.Copy(file, destTreatments);
            }

            // Get rid of the old file we're about to overwrite.
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            FileStream ofs = File.Create(file);
            StreamWriter ofp = new StreamWriter(ofs);

            foreach (KeyValuePair<string, string> kvp in localization)
            {
                string key = kvp.Key;
                string value = kvp.Value;

                if (!key.Contains("_DESCRIPTION"))
                {
                    if (key.Contains("EXM_") && selection.Equals("exams"))
                    {
                        ofp.WriteLine(value);
                    }
                    else if (key.Contains("TRT_") && selection.Equals("treatments"))
                    {
                        ofp.WriteLine(value);
                    }
                    
                }
            }

            ofp.Flush();
            ofp.Close();
        }

        // Generates two output files per department: the diagnoses for each department and the data for each department.
        static void generateOutputFile(string path, Department dept, string deptID)
        {
            string file = path + "\\Dept-Diagnoses\\" + dept.name + "-Diagnoses.txt";
            string dataFile = path + "\\Dept-Data\\" + dept.name + "-Data.txt";

            // Delete the existing file.
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            if (File.Exists(dataFile))
            {
                File.Delete(dataFile);
            }

            // File stream and pointer for the main output file.
            FileStream ofs = File.Create(file);
            StreamWriter ofp = new StreamWriter(ofs);

            // File stream and pointer for the data file.
            FileStream ofsData = File.Create(dataFile);
            StreamWriter ofpData = new StreamWriter(ofsData);

            // Extract only the diagnoses being evaluated in this department.
            List<Diagnosis> deptDiagnoses = diagnoses.Where(diagnosis => diagnosis.department.Equals(deptID)).ToList();

            // Substitute the name and description of each diagnosis within this department with its localization string.
            substituteIDs(deptDiagnoses);

            // Sort the list alphabetically.
            deptDiagnoses.Sort(delegate(Diagnosis x, Diagnosis y)
            {
                if (x.name == null && y.name == null) 
                    return 0;
                else if (x.name == null) 
                    return -1;
                else if (y.name == null) 
                    return -1;
                else 
                    return x.name.CompareTo(y.name);
            });

            // Tally the total number of diagnoses in this department.
            dept.totalDiagnoses = deptDiagnoses.Count();
            
            //ofp.WriteLine("# {0}\n", dept.name);

            // Loop through all diagnoses in this department and write them to the file, with all their relevant information.
            for (int i = 0; i < deptDiagnoses.Count; i++)
            {
                // Writes the name, occurrence, payment, and description of the diagnosis.
                ofp.WriteLine("## {0}", normalize(deptDiagnoses[i].name));
                ofp.WriteLine("__Occurrence: {0} | Base Payment: ${1}__", deptDiagnoses[i].occurrence, deptDiagnoses[i].payment);
                ofp.WriteLine("{0}", deptDiagnoses[i].description);
                ofp.WriteLine("");

                // Sort the symptoms list by probability, with the symptoms most likely to appear being at the top.
                deptDiagnoses[i].symptoms.Sort(delegate(Symptom x, Symptom y)
                {
                    return y.probability.CompareTo(x.probability);
                });

                // Writes out all symptoms and their relevant information.
                ofp.WriteLine("__Symptoms__");
                ofp.WriteLine("");

                bool isSerious = false;
                bool isFatal = false;
                bool notMarkedAsSerious = true;
                bool notMarkedAsFatal = true;

                // Loop through each symptom for the current disease.
                for (int j = 0; j < deptDiagnoses[i].symptoms.Count; j++)
                {
                    Symptom symptom = deptDiagnoses[i].symptoms[j];

                    // Print the symptom name, probability, and hazard.
                    // If the symptom cannot cause a collapse, use this format.
                    if (symptom.collapseSymptom.Equals("N/A") && symptom.deathStart.Equals("N/A"))
                    {
                        ofp.WriteLine("+ {0} ({1}% of cases | {2} Hazard)", normalize(localization[symptom.name]), symptom.probability, symptom.hazard);
                    }
                    // If the symptom can cause a collapse, then use this format.
                    else if (!symptom.collapseSymptom.Equals("N/A"))
                    {
                        ofp.WriteLine("+ {0} ({1}% of cases | {2} Hazard | __Serious__)", normalize(localization[symptom.name]), symptom.probability, symptom.hazard);
                        isSerious = true;
                    }
                    // If the symptom causes death directly, state that it is fatal.
                    else if (!symptom.deathStart.Equals("N/A"))
                    {
                        ofp.WriteLine("+ {0} ({1}% of cases | {2} Hazard | __Fatal__)", normalize(localization[symptom.name]), symptom.probability, symptom.hazard);
                        isFatal = true;
                    }

                    // Prints out the possible exams that will reveal the symptom.
                    if (symptom.examinations.Count == 1)
                    {
                        ofp.WriteLine("    + Revealed with __{0}__.", examPairs[localization[symptom.examinations[0]]]);
                    }
                    else if (symptom.examinations.Count == 2)
                    {
                        ofp.WriteLine("    + Revealed with __{0}__ or __{1}__.", examPairs[localization[symptom.examinations[0]]], examPairs[localization[symptom.examinations[1]]]);
                    }
                    else
                    {
                        ofp.WriteLine("    + Revealed with __{0}__, __{1}__, or __{2}__.", examPairs[localization[symptom.examinations[0]]], examPairs[localization[symptom.examinations[1]]], examPairs[localization[symptom.examinations[2]]]);
                    }

                    // Prints out the treatment used to suppress the symptom.
                    // TODO: Will need to be updated if multiple treatment types are added in the future.
                    if (symptom.treatments.Count > 0)
                    {
                        ofp.WriteLine("    + Treated with __{0}__.", treatmentPairs[localization[symptom.treatments[0]]]);
                    }
                    else
                    {
                        ofp.WriteLine("    + Cannot be treated.");
                    }

                    // If the symptom is serious, denote the collapse symptom and its hours.
                    if (isSerious)
                    {
                        string collapseTime = symptom.collapseStart + " to " + symptom.collapseEnd;
                        ofp.WriteLine("    + Can lead to __{0}__ if left untreated for __{1}__ hours.", localization[symptom.collapseSymptom].ToLower(), collapseTime);

                        // Used for data collection purposes to keep track of the total number of serious diagnoses.
                        if (notMarkedAsSerious)
                        {
                            dept.totalSeriousDiagnoses++;
                            notMarkedAsSerious = false;
                        }

                        // Used for data collection purposes.
                        dept.urgentExamUsage.TryAdd(symptom.examinations[0], 0);
                        dept.urgentExamUsage[symptom.examinations[0]] = dept.urgentExamUsage[symptom.examinations[0]] + 1;
                    }
                    
                    // If the symptom is the main symptom - i.e. the "deciding" symptom that has a 100% of being present, note which exams uncover it
                    // for data purposes.
                    if (symptom.isMainSymptom)
                    {
                        dept.examUsage.TryAdd(symptom.examinations[0], 0);
                        dept.examUsage[symptom.examinations[0]] = dept.examUsage[symptom.examinations[0]] + 1;
                    }

                    // Similarly, if the symptom is fatal, denote the death hours.
                    if (isFatal)
                    {
                        string deathTime = symptom.deathStart + " to " + symptom.deathEnd;
                        ofp.WriteLine("    + Can lead to __death__ if left untreated for __{0}__ hours.", deathTime);

                        // Used for data collection purposes to keep track of all potentially-fatal diagnoses.
                        if (notMarkedAsFatal)
                        {
                            dept.totalFatalDiagnoses++;
                            notMarkedAsFatal = false;
                        }
                    }

                    isSerious = false;
                    isFatal = false;
                }

                notMarkedAsSerious = false;
                notMarkedAsFatal = false;

                ofp.WriteLine("");
            }

            // Write department data in the data file for this given department.
            ofpData.WriteLine(dept.name);
            ofpData.WriteLine("Total Diagnoses: {0} | Serious: {1} | Fatal: {2}", dept.totalDiagnoses, dept.totalSeriousDiagnoses, dept.totalFatalDiagnoses);
            
            // Write the frequency of exams which definitively diagnose a patient.
            ofpData.WriteLine("\nDeciding Exams:");

            foreach (KeyValuePair<string, int> exam in dept.examUsage)
            {
                string examName = exam.Key;
                int examFreq = exam.Value;

                ofpData.WriteLine("{0}: {1}", localization[examName], examFreq);
            }

            // Write the frequency of exams which uncover a symptom that can lead to collapse / death.
            ofpData.WriteLine("\nUrgent Exams:");

            foreach (KeyValuePair<string, int> exam in dept.urgentExamUsage)
            {
                string examName = exam.Key;
                int examFreq = exam.Value;

                ofpData.WriteLine("{0}: {1}", localization[examName], examFreq);
            }

            ofp.Flush();
            ofpData.Flush();
            ofp.Close();
            ofpData.Close();
        }

        static void substituteIDs(List<Diagnosis> deptDiagnoses)
        {
            foreach (Diagnosis d in deptDiagnoses)
            {
                d.name = localization[d.name];

                if (d.name.Contains("(R)"))
                {
                    d.name = d.name.Replace("(R)", "[R]");
                }

                if (d.name.Contains("(C)"))
                {
                    d.name = d.name.Replace("(C)", "[C]");
                }

                d.description = localization[d.description];
            }
        }

        // Makes corrections to grammar or spelling in any instance of a string. Typically used in disease names and descriptions.
        static string normalize(string temp)
        {
            TextInfo info = new CultureInfo("en-US", false).TextInfo;

            temp = info.ToTitleCase(temp);

            if (temp.Contains("And"))
            {
                temp = temp.Replace("And", "and");
            }

            if (temp.Contains("Of"))
            {
               temp = temp.Replace("Of", "of");
            }

            if (temp.Contains("The"))
            {
                temp = temp.Replace("The", "the");
            }

            if (temp.Contains("’S"))
            {
                temp = temp.Replace("’S", "’s");
            }

            if (temp.Contains("Hearbeat"))
            {
                temp = temp.Replace("Hearbeat", "Heartbeat");
            }

            if (temp.Contains(" Or "))
            {
                temp = temp.Replace(" Or ", " or ");
            }

            return temp;
        }

        // A generalized method that takes in a file path and a selection, and processes every file in that directory based on the selection.
        static void processAllFilesInDirectory(string path, string selection)
        {
            // Force the XmlReader to ignore comments.
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreComments = true;

            foreach (var file in System.IO.Directory.GetFiles(path))
            {
                // Create a new reader based on the current file being evaluated, with the settings that ignore comments.
                XmlReader reader = XmlReader.Create(file, readerSettings);

                // Pick a processing method based on the selection string.
                switch (selection)
                {
                    case "diagnosis":
                        processDiagnoses(reader);
                        break;
                    case "localization":
                        processLocalization(reader);
                        break;
                    case "symptom":
                        processSymptoms(reader);
                        break;
                }
            }
        }

        // Process all the diagnoses in the diagnosis file passed to the method.
        // This method will only set up a new instance of the Diagnosis class and add it to the Diagnoses list. It will not format it
        // by replacing ID tags with localization strings.
        static void processDiagnoses(XmlReader file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);

            // Get a reference to the Database.
            XmlNode root = doc.FirstChild;

            // Process each diagnosis inside of a given diagnosis file.
            if (root.HasChildNodes)
            {
                foreach (XmlNode node in root.ChildNodes)
                {
                    Diagnosis diagnosis = new Diagnosis();
                    
                    diagnosis.name = node.Attributes[0].Value;
                    
                    initDiagnosis(node, diagnosis);
                    
                    diagnoses.Add(diagnosis);
                }
            }
        }

        // This method initializes a diagnosis will all relevant information gathered from the current XML file.
        static void initDiagnosis(XmlNode medicalCondition, Diagnosis diagnosis)
        {
            foreach (XmlNode node in medicalCondition.ChildNodes)
            {
                if (node.Name.Equals("AbbreviationLocID"))
                {
                    diagnosis.description = node.InnerText;
                }
                else if (node.Name.Equals("OccurrenceRef"))
                {
                    diagnosis.occurrence = normalizeOccurence(node.InnerText);
                }
                else if (node.Name.Equals("Symptoms"))
                {
                    initDiagnosisSymptoms(node, diagnosis.symptoms);
                }
                else if (node.Name.Equals("DepartmentRef"))
                {
                    diagnosis.department = node.InnerText;
                }
                else if (node.Name.Equals("InsurancePayment"))
                {
                    diagnosis.payment = Int32.Parse(node.InnerText);
                }
            }
        }

        // Makes the occurrence label nice to read.
        static string normalizeOccurence(string input)
        {
            TextInfo info = new CultureInfo("en-US", false).TextInfo;
            string output = input;

            if (output.Contains("OCCURRENCE_K"))
            {
                output = "Very Rare";
            }

            if (output.Contains("OCCURRENCE_"))
            {
                output = output.Replace("OCCURRENCE_", "");
            }

            if (output.Contains("OR_"))
            {
                output = output.Replace("OR_", "");
            }

            if (output.Contains("KN_"))
            {
                output = output.Replace("KN_", "");
            }

            if (output.Contains("SL_"))
            {
                output = output.Replace("SL_", "");
            }

            output = output.ToLower();

            // Normalize straggling strings. These are primarily used in modded diagnoses.
            switch (output)
            {
                case "veryrare":
                    output = "very rare";
                    break;
                case "extremelyrare":
                    output = "extremely rare";
                    break;
                case "ultrarare":
                    output = "ultra rare";
                    break;
            }
            
            return info.ToTitleCase(output);
        }

        // This method runs through all symptoms that pertain to a single diagnosis and adds it to that diagnosis' symptom list.
        static void initDiagnosisSymptoms(XmlNode root, List<Symptom> symptoms)
        {
            // Run through each individual symptom. This would be "GameDBSymptomRules"
            foreach (XmlNode node in root.ChildNodes)
            {
                Symptom symptom = new Symptom();

                // Run through each field within the given symptom, i.e. the probability and DBSymptomRef
                foreach (XmlNode symptomStat in node.ChildNodes)
                {
                    if (symptomStat.Name.Equals("ProbabilityPercent"))
                    {
                        symptom.probability = Int32.Parse(symptomStat.InnerText);
                    }
                    else if(symptomStat.Name.Equals("GameDBSymptomRef"))
                    {
                        symptom.name = symptomStat.InnerText;

                        setupSymptom(symptom);
                    }
                }

                symptoms.Add(symptom);
            }
        }

        // Copy over all relevant data from the symptom dictionary to the symptom about to be added to the active diagnosis.
        static void setupSymptom(Symptom symptom)
        {
            Symptom info = symptoms[symptom.name];

            symptom.discomfort = info.discomfort;
            symptom.patientComplains = info.patientComplains;
            symptom.hazard = info.hazard;
            symptom.collapseStart = info.collapseStart;
            symptom.collapseEnd = info.collapseEnd;
            symptom.deathStart = info.deathStart;
            symptom.deathEnd = info.deathEnd;
            symptom.collapseSymptom = info.collapseSymptom;
            symptom.mobility = info.mobility;
            symptom.examinations = info.examinations;
            symptom.treatments = info.treatments;
            symptom.shameLevel = info.shameLevel;
            symptom.isMainSymptom = info.isMainSymptom;
        }

        // Processes all files int he Localization folder.
        static void processLocalization(XmlReader file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);

            // Get a reference to the ids and corresponding text fields.
            XmlNodeList ids = doc.GetElementsByTagName("LocID");
            XmlNodeList text = doc.GetElementsByTagName("Text");

            // Process each diagnosis inside of a given diagnosis file.
            if (ids.Count == text.Count)
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    // Prevent the localization dictionary from filling up with many useless localization strings first, then
                    // check to see if the dictionary already has the key value pair before adding it.
                    if (isValidLocalizationString(ids[i].InnerText))
                    {
                        localization.TryAdd(ids[i].InnerText, text[i].InnerText);
                        
                        // Add to a special localization for output purposes.
                        if (!ids[i].InnerText.Contains("_DESCRIPTION"))
                        {
                            nameList.Add(ids[i].InnerText);
                        }
                        else if (ids[i].InnerText.Contains("_DESCRIPTION"))
                        {
                            descList.Add(ids[i].InnerText);
                        }
                    }
                }
            }
            // If the ids and text counts are not equal, we cannot proceed, so throw an error.
            else
            {
                Console.WriteLine("CRITICAL ERROR: IDs and Text list lengths not equal in processLocalization()");
                System.Environment.Exit(1);
            }
        }

        // This method only returns true is the current localization id contains an identifier that shows it is either a diagnosis, trauma, exam, symptom, or treatment.
        // All other localization strings get ignored.
        static bool isValidLocalizationString(string text)
        {
            if (text.Contains("DIA_") || text.Contains("TRM") || text.Contains("EXM_") || text.Contains("SYM_") || text.Contains("TRT_"))
            {
                return true;
            }

            return false;
        }

        // Processes all the files found in the Symptoms folder.
        static void processSymptoms(XmlReader file)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);

            // Get a reference to the Database.
            XmlNode root = doc.FirstChild;

            // Process all symptoms within the folder, adding them to the symptom dictionary with their ID name as their key.
            if (root.HasChildNodes)
            {
                foreach (XmlNode node in root.ChildNodes)
                {
                    Symptom symptom = new Symptom();

                    symptom.name = node.Attributes[0].Value;

                    initSymptom(node, symptom);

                    symptoms.TryAdd(symptom.name, symptom);
                }
            }
        }

        // Initialize all of the symptom's member fields - except probability - from the symptom file.
        static void initSymptom(XmlNode root, Symptom symptom)
        {
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.Name.Equals("DescriptionLocID"))
                {
                    symptom.description = node.InnerText;
                }
                else if (node.Name.Equals("DiscomfortLevel"))
                {
                    symptom.discomfort = node.InnerText;
                }
                else if (node.Name.Equals("PatientComplains"))
                {
                    symptom.patientComplains = bool.Parse(node.InnerText);
                }
                else if (node.Name.Equals("Hazard"))
                {
                    symptom.hazard = node.InnerText;
                }
                else if (node.Name.Equals("RiskOfCollapseStartHours"))
                {
                    symptom.collapseStart = node.InnerText;
                }
                else if (node.Name.Equals("RiskOfCollapseEndHours"))
                {
                    symptom.collapseEnd = node.InnerText;
                }
                else if (node.Name.Equals("RiskOfDeathStartHours"))
                {
                    symptom.deathStart = node.InnerText;
                }
                else if (node.Name.Equals("RiskOfDeathEndHours"))
                {
                    symptom.deathEnd = node.InnerText;
                }
                else if (node.Name.Equals("CollapseSymptomRef"))
                {
                    symptom.collapseSymptom = node.InnerText;
                }
                else if (node.Name.Equals("PatientMobility"))
                {
                    symptom.mobility = node.InnerText;
                }
                // Get all the possible examinations and add them to the exam list.
                else if (node.Name.Equals("Examinations"))
                {
                   foreach (XmlNode exam in node.ChildNodes)
                   {
                       symptom.examinations.Add(exam.InnerText);
                   } 
                }
                // Get all viable treatments and add them to the treatment list.
                else if (node.Name.Equals("Treatments"))
                {
                    foreach (XmlNode treatment in node.ChildNodes)
                   {
                       symptom.treatments.Add(treatment.InnerText);
                   } 
                }
                else if (node.Name.Equals("ShameLevel"))
                {
                    symptom.shameLevel = Int32.Parse(node.InnerText);
                }
                else if (node.Name.Equals("IsMainSymptom"))
                {
                    symptom.isMainSymptom = bool.Parse(node.InnerText);
                }
            }
        }

        static void Main(string[] args)
        {
            // Set up the list and dictionaries used to catalog all the data necessary for the output files.
            diagnoses = new List<Diagnosis>();
            symptoms = new Dictionary<string, Symptom>();
            localization = new Dictionary<string, string>();
            examPairs = new Dictionary<string, string>();
            treatmentPairs = new Dictionary<string, string>();
            nameList = new List<string>();
            descList = new List<string>();

            Scrape();

            Console.WriteLine("Yay! The Scraper chugged along diligently and got all the data it wanted! Way to go, little scraper!");
        }
    }
}
