using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using UNITe.CommonLibrary;
using UNITe.CommonLibrary.SearchForm;
using UNITe.Database;
using UNITe.Database.Report;
using UNITe.Database.XMLDataDefinition;
using UNITe.InformationInterface.Common;
using UNITe.InformationInterface.Controls;

public partial class CodeBehindClass
{
    private Form mainForm;
    private UNITe.InformationInterface.Controls.UNITePanel CourseEnrolmentPnl;
    private UNITe.InformationInterface.Controls.UNITePanel AllConnectedEnrolmentsPanel;
    private System.Windows.Forms.ComboBox comboBox1;
    private System.Windows.Forms.ComboBox comboBox2;

    public CodeBehindClass(Control control)
    {
        control.VisibleChanged += new EventHandler(control_VisibleChanged);
    }

    private void control_VisibleChanged(object sender, EventArgs e)
    {
        Control control = sender as Control;
        if (control.Visible)
        {
            control.VisibleChanged -= new EventHandler(control_VisibleChanged);
            this.mainForm = control.FindForm();

            this.comboBox1 = this.mainForm.Controls.Find("comboBox1", true)[0] as System.Windows.Forms.ComboBox;
            this.comboBox2 = this.mainForm.Controls.Find("comboBox2", true)[0] as System.Windows.Forms.ComboBox;
            this.CourseEnrolmentPnl = this.mainForm.Controls.Find("CourseEnrolmentPnl", true)[0] as UNITe.InformationInterface.Controls.UNITePanel;
            this.AllConnectedEnrolmentsPanel = this.mainForm.Controls.Find("AllConnectedEnrolmentsPanel", true)[0] as UNITe.InformationInterface.Controls.UNITePanel;

            Button btnCreateCourseEnrolment = new Button();
            btnCreateCourseEnrolment.Text = "Create Course Enrolment";
            btnCreateCourseEnrolment.Dock = DockStyle.Fill;
            btnCreateCourseEnrolment.Click += new System.EventHandler(this.btnCreateCourseEnrolment_Clicked);

            this.CourseEnrolmentPnl.Controls.Add(btnCreateCourseEnrolment);

            //get the database connection
            UNITe.Database.Connection connection = UNITe.InterOp.Globals.Connection;
            //create an instance of the export library - used throughout this script
            UNITe.Database.Report.Export export = new UNITe.Database.Report.Export();

            System.Collections.Generic.List<UNITe.CommonLibrary.SearchForm.SearchFormField> StatusSearchParams = new System.Collections.Generic.List<UNITe.CommonLibrary.SearchForm.SearchFormField>();

            UNITe.CommonLibrary.SearchForm.SearchFormField searchFields = new UNITe.CommonLibrary.SearchForm.SearchFormField();
            //set the class/property/value
            searchFields.ClassName = "valid";
            searchFields.Property = "default";
            searchFields.Value = "-1";
            //add the field to the list
            StatusSearchParams.Add(searchFields);

            //get the data table back
            DataTable dt = export.ExportDataTable(connection, "rg.ii.appenrstatus", StatusSearchParams);

            // DataRow dataRow = dt.NewRow();

            //  dataRow["Name"] = "(none)";
            //  dataRow["Value"] = "";
            //  dataRow["DefaultValue"] = "";

            //  dt.Rows.InsertAt(dataRow, 0);
            //  dt.AcceptChanges();

            this.comboBox1.ValueMember = "Code";
            this.comboBox1.DisplayMember = "Name";
            this.comboBox1.DataSource = dt;
            if (dt.Rows.Count > 1)
            {
                this.comboBox1.SelectedValue = dt.Rows[1]["DefaultValue"].ToString();
            }

            UNITe.Database.BusinessClasses.LookupValidValues lookupS = new UNITe.Database.BusinessClasses.LookupValidValues(UNITe.InterOp.Globals.Connection);
            UNITe.Business.Helper.LookupValidValuesParameter domainParam = new UNITe.Business.Helper.LookupValidValuesParameter();
            domainParam.Type = "feeband";
            domainParam.IsDefaultValue = "-1";
            System.Collections.Generic.List<UNITe.Business.Helper.LookupValidValues> feeBands = lookupS.FindLookupValidValues(domainParam);

            this.comboBox2.ValueMember = "Code";
            this.comboBox2.DisplayMember = "Name";
            this.comboBox2.DataSource = feeBands;


        }
    }

    private void btnCreateCourseEnrolment_Clicked(object sender, EventArgs e)
    {
        UNITeDataManager dmEnrolment = null;

        foreach (UNITeDataManager dm in UNITeDataManager.DataManagers)
        {
            if (dm.Name == "uniTeDataManager1")
            {
                dmEnrolment = dm;
                break;
            }
        }

        string route_id = dmEnrolment.Read("moduleapplication1", "route").ToString();
        DataTable dt = getModuleDetailsForEnrolment(route_id);
        int i = 0;
        if (dt.Rows.Count > 0)
        {
            string course_id = "";
            string course_name = "";
            string year_id = "";
            string year_name = "";
            List<string> module_id = new List<string>();

            UNITe.Business.Helper.StudentHESA initialStudentHESA = getStudentHESA(dmEnrolment.Read("studenthesa1", "id").ToString());
            UNITe.Business.Helper.ModuleHESA initialModule = null;
            UNITe.Business.Helper.ModuleFee moduleFee = null;
            string fee_region = "";
            string modeOfStudy = "";
            string enrolmentCreated = String.Empty;
            bool createEnrFee = false;
            string yearparentEnr = string.Empty;
            string moduleParentEnr = string.Empty;

            foreach (DataRow dr in dt.Rows)
            {
                i = i + 1;
                if (dr["CourseName"] != null && course_name == string.Empty)
                {
                    course_name = dr["CourseName"].ToString();
                    course_id = dr["CourseId"].ToString();
                    if (course_id != null)
                    {
                        initialModule = getModule(course_id);
                        if (dmEnrolment != null)
                        {
                            if (dmEnrolment != null && dmEnrolment.Read("studenthesa1", "feeregion") != null)
                            {
                                fee_region = dmEnrolment.Read("studenthesa1", "feeregion").ToString();
                            }
                            if (dmEnrolment != null && dmEnrolment.Read("studentapplication1", "mode") != null)
                            {
                                modeOfStudy = dmEnrolment.Read("studentapplication1", "mode").ToString();
                            }
                            if (initialModule != null)
                            {
                                enrolmentCreated = createEnrolment(dmEnrolment, initialStudentHESA, initialModule, fee_region, modeOfStudy, "");
                                List<UNITe.Business.Helper.Unit> units = getUnitDetailsForAssessments(initialModule.Id);
                                foreach (UNITe.Business.Helper.Unit assUnit in units)
                                {
                                    createAssessmentRecords(enrolmentCreated, assUnit);
                                }
                                moduleFee = getModuleFeeRecord(course_id);
                                if (moduleFee != null)
                                {
                                    createEnrFee = createEnrolmentFee(moduleFee, dmEnrolment);
                                }
                            }
                        }
                    }
                }


                if (string.IsNullOrEmpty(yearparentEnr)) yearparentEnr = enrolmentCreated;

                if (dr["YearName"] != null && year_name == string.Empty)
                {
                    year_name = dr["YearName"].ToString();
                    year_id = dr["YearId"].ToString();
                    initialModule = null;
                    if (!string.IsNullOrEmpty(year_id))
                    {
                        initialModule = getModule(year_id);
                        if (dmEnrolment != null)
                        {
                            if (dmEnrolment != null && dmEnrolment.Read("studenthesa1", "feeregion") != null)
                            {
                                fee_region = dmEnrolment.Read("studenthesa1", "feeregion").ToString();
                            }
                            if (initialModule != null)
                            {
                                enrolmentCreated = createEnrolment(dmEnrolment, initialStudentHESA, initialModule, fee_region, modeOfStudy, yearparentEnr);
                                List<UNITe.Business.Helper.Unit> units = getUnitDetailsForAssessments(initialModule.Id);
                                foreach (UNITe.Business.Helper.Unit assUnit in units)
                                {
                                    createAssessmentRecords(enrolmentCreated, assUnit);
                                }
                                moduleFee = getModuleFeeRecord(year_id);
                                if (moduleFee != null)
                                {
                                    createEnrFee = createEnrolmentFee(moduleFee, dmEnrolment);
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(moduleParentEnr)) moduleParentEnr = enrolmentCreated;

                if (dr["ModuleName"] != null)
                {
                    module_id.Add(dr["ModuleId"].ToString());
                    initialModule = null;
                    if (dr["ModuleId"] != null)
                    {
                        initialModule = getModule(dr["ModuleId"].ToString());
                        if (dmEnrolment != null)
                        {
                            if (dmEnrolment != null && dmEnrolment.Read("studenthesa1", "feeregion") != null)
                            {
                                fee_region = dmEnrolment.Read("studenthesa1", "feeregion").ToString();
                            }
                            if (initialModule != null)
                            {
                                enrolmentCreated = createEnrolment(dmEnrolment, initialStudentHESA, initialModule, fee_region, modeOfStudy, moduleParentEnr);
                                List<UNITe.Business.Helper.Unit> units = getUnitDetailsForAssessments(initialModule.Id);
                                foreach (UNITe.Business.Helper.Unit assUnit in units)
                                {
                                    createAssessmentRecords(enrolmentCreated, assUnit);
                                }
                                moduleFee = getModuleFeeRecord(dr["ModuleId"].ToString());
                                if (moduleFee != null)
                                {
                                    createEnrFee = createEnrolmentFee(moduleFee, dmEnrolment);
                                }
                            }
                        }
                    }
                }
            }

            if (i == 1)
            {
                MessageBox.Show(i.ToString() + " enrolment created");
            }
            else MessageBox.Show(i.ToString() + " enrolments created");
        }
        else MessageBox.Show("No course associated with the Application");
    }

    protected UNITe.Business.Helper.ModuleFee getModuleFeeRecord(string module_id)
    {
        UNITe.Database.BusinessClasses.ModuleFee modulefeeQuery = new UNITe.Database.BusinessClasses.ModuleFee(UNITe.InterOp.Globals.Connection);
        UNITe.Business.Helper.ModuleFeeParameter moduleFeeParam = new UNITe.Business.Helper.ModuleFeeParameter();
        moduleFeeParam.FeeBand = comboBox2.SelectedValue.ToString();

        moduleFeeParam.Module = module_id;

        System.Collections.Generic.List<UNITe.Business.Helper.ModuleFee> moduleFees = modulefeeQuery.FindModuleFee(moduleFeeParam);
        //iterate through list and display id of each found record- note all other properties are available
        if (moduleFees != null)
        {
            if (moduleFees.Count > 0)
            {
                return moduleFees[0];
            }
        }

        return null;
    }

    private UNITe.Business.Helper.ModuleHESA getModule(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        //create a business class object to access the database
        UNITe.Database.BusinessClasses.ModuleHESA moduleQuery = new UNITe.Database.BusinessClasses.ModuleHESA(UNITe.InterOp.Globals.Connection);
        UNITe.Business.Helper.ModuleHESA foundModule = moduleQuery.GetModuleHESA(id);

        return foundModule;
    }

    private UNITe.Business.Helper.StudentHESA getStudentHESA(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        //create a business class object to access the database
        UNITe.Database.BusinessClasses.StudentHESA studentHESAQuery = new UNITe.Database.BusinessClasses.StudentHESA(UNITe.InterOp.Globals.Connection);
        UNITe.Business.Helper.StudentHESA foundStudentHESA = studentHESAQuery.GetStudentHESA(id);

        return foundStudentHESA;
    }
    private string createEnrolment(UNITeDataManager dmEnrolment, UNITe.Business.Helper.StudentHESA initialStudentHESA, UNITe.Business.Helper.ModuleHESA initialModule, string c68, string mode, string parent)
    {
        UNITe.Database.BusinessClasses.Enrolment bcEnrolment = new UNITe.Database.BusinessClasses.Enrolment(UNITe.InterOp.Globals.Connection);
        UNITe.Business.Helper.EnrolmentParameter enrParam = new UNITe.Business.Helper.EnrolmentParameter();
        enrParam.Module = initialModule.Id;
        enrParam.Student = initialStudentHESA.Id;
        List<UNITe.Business.Helper.Enrolment> enrs = bcEnrolment.FindEnrolment(enrParam);
        if (enrs.Count > 0) return "";

        dmEnrolment.Create("enrolmenthesa1");
        //student defaulting
        if (initialStudentHESA.PreviousSchool != null) dmEnrolment.Update("enrolmenthesa1", "enrolmentschool", initialStudentHESA.PreviousSchool);
        if (initialStudentHESA.CurrentEmployer != null) dmEnrolment.Update("enrolmenthesa1", "enrolmentemployer", initialStudentHESA.CurrentEmployer);
        if (initialStudentHESA.CurrentSponsor != null) dmEnrolment.Update("enrolmenthesa1", "enrolmentsponsor", initialStudentHESA.CurrentSponsor);
        if (initialStudentHESA.CurrentFundingBody != null) dmEnrolment.Update("enrolmenthesa1", "enrolmentfundingbody", initialStudentHESA.CurrentFundingBody);
        dmEnrolment.Update("enrolmenthesa1", "overseas", initialStudentHESA.IsOverseas);
        dmEnrolment.Update("enrolmenthesa1", "skeleton", initialModule.IsSkeletonEnrolment);
        dmEnrolment.Update("enrolmenthesa1", "skeleton", initialModule.IsSkeletonEnrolment);
        dmEnrolment.Update("enrolmenthesa1", "mode", mode);
        dmEnrolment.Update("enrolmenthesa1", "parent", parent);
        if (initialModule.Reference != null) dmEnrolment.Update("enrolmenthesa1", "reference", initialModule.Reference);
        if (initialModule.Name != null) dmEnrolment.Update("enrolmenthesa1", "name", initialModule.Name);
        if (initialModule.Start != null) dmEnrolment.Update("enrolmenthesa1", "start", initialModule.Start);
        if (initialModule.End != null) dmEnrolment.Update("enrolmenthesa1", "end", initialModule.End);
        if (initialModule.Duration != null) dmEnrolment.Update("enrolmenthesa1", "duration", initialModule.Duration);
        if (initialModule.Type != null) dmEnrolment.Update("enrolmenthesa1", "type", initialModule.Type);
        if (initialModule.Partner != null) dmEnrolment.Update("enrolmenthesa1", "partner", initialModule.Partner);
        if (initialModule.Site != null) dmEnrolment.Update("enrolmenthesa1", "site", initialModule.Site);
        if (string.IsNullOrEmpty(mode))
        {
            if (initialModule.Mode != null) dmEnrolment.Update("enrolmenthesa1", "mode", initialModule.Mode);
        }
        if (initialModule.Level != null) dmEnrolment.Update("enrolmenthesa1", "level", initialModule.Level);
        if (initialModule.Subject != null) dmEnrolment.Update("enrolmenthesa1", "subject", initialModule.Subject);
        if (initialModule.Method != null) dmEnrolment.Update("enrolmenthesa1", "method", initialModule.Method);
        if (initialModule.Tutor != null) dmEnrolment.Update("enrolmenthesa1", "enrolmenttutor", initialModule.Tutor);
        if (initialModule.FeeBand != null) dmEnrolment.Update("enrolmenthesa1", "feeband", initialModule.FeeBand);
        if (initialModule.CreditValue != null) dmEnrolment.Update("enrolmenthesa1", "creditvalue", initialModule.CreditValue);
        if (initialModule.ResultLevel != null) dmEnrolment.Update("enrolmenthesa1", "resultlevel", initialModule.ResultLevel);
        if (initialModule.Id != null) dmEnrolment.Update("enrolmenthesa1", "module", initialModule.Id);
        dmEnrolment.Update("enrolmenthesa1", "enrolmenthesa", initialModule.IsIncludeInHESA);
        dmEnrolment.Update("enrolmenthesa1", "status", this.comboBox1.SelectedValue);
        if (initialStudentHESA.C6FEStudentMarker != null) dmEnrolment.Update("enrolmenthesa1", "c6", initialStudentHESA.C6FEStudentMarker);
        if (initialStudentHESA.C12Domicile != null) dmEnrolment.Update("enrolmenthesa1", "c12", initialStudentHESA.C12Domicile);
        if (initialStudentHESA.C15DisabilityAllowance != null) dmEnrolment.Update("enrolmenthesa1", "c15", initialStudentHESA.C15DisabilityAllowance);
        if (initialStudentHESA.C18LastAttended != null) dmEnrolment.Update("enrolmenthesa1", "c18", initialStudentHESA.C18LastAttended);
        if (initialStudentHESA.C19YearLeftLastInstitution != null) dmEnrolment.Update("enrolmenthesa1", "c19", initialStudentHESA.C19YearLeftLastInstitution);
        if (initialStudentHESA.C21HighestQualificationOnEntry != null) dmEnrolment.Update("enrolmenthesa1", "c21", initialStudentHESA.C21HighestQualificationOnEntry);
        if (initialStudentHESA.C23AASLevelPointsScore != null) dmEnrolment.Update("enrolmenthesa1", "c23", initialStudentHESA.C23AASLevelPointsScore);
        if (initialStudentHESA.C24HighersPointsScore != null) dmEnrolment.Update("enrolmenthesa1", "c24", initialStudentHESA.C24HighersPointsScore);
        if (initialStudentHESA.C25OccupationCode != null) dmEnrolment.Update("enrolmenthesa1", "c25", initialStudentHESA.C25OccupationCode);
        if (initialStudentHESA.C28SpecialStudents != null) dmEnrolment.Update("enrolmenthesa1", "c28", initialStudentHESA.C28SpecialStudents);
        if (initialStudentHESA.C31TermTimeAccommodation != null) dmEnrolment.Update("enrolmenthesa1", "c31", initialStudentHESA.C31TermTimeAccommodation);
        if (initialStudentHESA.C76JACSPGCESubjectOfUndergraduateDegree != null) dmEnrolment.Update("enrolmenthesa1", "c76jacs", initialStudentHESA.C76JACSPGCESubjectOfUndergraduateDegree);
        if (initialStudentHESA.C77PGCEClassificationOfUndergraduateDegree != null) dmEnrolment.Update("enrolmenthesa1", "c77", initialStudentHESA.C77PGCEClassificationOfUndergraduateDegree);
        if (initialStudentHESA.C148UCASNumber != null) dmEnrolment.Update("enrolmenthesa1", "c148", initialStudentHESA.C148UCASNumber);
        if (initialStudentHESA.C157AAndASLevels != null) dmEnrolment.Update("enrolmenthesa1", "c157", initialStudentHESA.C157AAndASLevels);
        if (initialStudentHESA.C158SCEHighersAndCSYS != null) dmEnrolment.Update("enrolmenthesa1", "c158", initialStudentHESA.C158SCEHighersAndCSYS);
        if (initialStudentHESA.C159VocationalQualsLevel3Advanced != null) dmEnrolment.Update("enrolmenthesa1", "c159", initialStudentHESA.C159VocationalQualsLevel3Advanced);
        if (initialStudentHESA.C173NHSEmployer != null) dmEnrolment.Update("enrolmenthesa1", "c173", initialStudentHESA.C173NHSEmployer);
        if (initialModule.Period != null) dmEnrolment.Update("enrolmenthesa1", "c30", initialModule.Period);
        if (initialModule.C43SubjectOfQualificationAim != null) dmEnrolment.Update("enrolmenthesa1", "c43", initialModule.C43SubjectOfQualificationAim);
        if (initialModule.C44SubjectOfQualificationAim2 != null) dmEnrolment.Update("enrolmenthesa1", "c44", initialModule.C44SubjectOfQualificationAim2);
        if (initialModule.C45SubjectOfQualificationAim3 != null) dmEnrolment.Update("enrolmenthesa1", "c45", initialModule.C45SubjectOfQualificationAim3);
        if (initialModule.C46ProportionIndicator != null) dmEnrolment.Update("enrolmenthesa1", "c46", initialModule.C46ProportionIndicator);
        if (initialModule.C57TeachingQualificationSoughtSubject1 != null) dmEnrolment.Update("enrolmenthesa1", "c57", initialModule.C57TeachingQualificationSoughtSubject1);
        if (initialModule.C58TeachingQualificationSoughtSubject2 != null) dmEnrolment.Update("enrolmenthesa1", "c58", initialModule.C58TeachingQualificationSoughtSubject2);
        if (initialModule.C59TeachingQualificationSoughtSubject3 != null) dmEnrolment.Update("enrolmenthesa1", "c59", initialModule.C59TeachingQualificationSoughtSubject3);
        if (initialModule.C70ModeOfStudy != null) dmEnrolment.Update("enrolmenthesa1", "c70", initialModule.C70ModeOfStudy);
        if (initialModule.C71LocationOfStudy != null) dmEnrolment.Update("enrolmenthesa1", "c71", initialModule.C71LocationOfStudy);
        if (initialModule.C74StudentFTE != null) dmEnrolment.Update("enrolmenthesa1", "c74", initialModule.C74StudentFTE);
        if (initialModule.C153TypeOfProgrammeYear != null) dmEnrolment.Update("enrolmenthesa1", "c153", initialModule.C153TypeOfProgrammeYear);
        if (initialModule.C164UFIPlace != null) dmEnrolment.Update("enrolmenthesa1", "c164", initialModule.C164UFIPlace);
        dmEnrolment.Update("enrolmenthesa1", "hesesnewentrant", initialModule.IsHESESNewEntrant);
        dmEnrolment.Update("enrolmenthesa1", "hesesscitt", initialModule.IsSchoolCentredITT);
        if (initialModule.N13TrainingTypeCode != null) dmEnrolment.Update("enrolmenthesa1", "n13", initialModule.N13TrainingTypeCode);
        if (initialModule.N14CourseOfStudyCode != null) dmEnrolment.Update("enrolmenthesa1", "c14", initialModule.N14CourseOfStudyCode);
        if (initialModule.N21FinalExamDatePassDate != null) dmEnrolment.Update("enrolmenthesa1", "n21", initialModule.N21FinalExamDatePassDate);
        if (initialModule.C219ImpliedRateOfCouncilPartialFunding != null) dmEnrolment.Update("enrolmenthesa1", "c219", initialModule.C219ImpliedRateOfCouncilPartialFunding);
        if (initialModule.C220aGovernmentInitiatives != null) dmEnrolment.Update("enrolmenthesa1", "c220a", initialModule.C220aGovernmentInitiatives);
        if (initialModule.C220bGovernmentInitiatives != null) dmEnrolment.Update("enrolmenthesa1", "c220b", initialModule.C220bGovernmentInitiatives);
        if (initialModule.C222NumberOfUnitsToAchieveFullQualification != null) dmEnrolment.Update("enrolmenthesa1", "c222", initialModule.C222NumberOfUnitsToAchieveFullQualification);
        if (initialModule.C225FranchisedOutArrangements != null) dmEnrolment.Update("enrolmenthesa1", "c225", initialModule.C225FranchisedOutArrangements);
        if (initialModule.C226EmployerRole != null) dmEnrolment.Update("enrolmenthesa1", "c226", initialModule.C226EmployerRole);
        dmEnrolment.Update("enrolmenthesa1", "includeinncb", initialModule.IsIncludeInNCB);
        dmEnrolment.Update("enrolmenthesa1", "welshforadults", initialModule.IsWelshForAdults);
        dmEnrolment.Update("enrolmenthesa1", "c68", c68);

        return dmEnrolment.Read("enrolmenthesa1", "id").ToString();
    }

    private bool createEnrolmentFee(UNITe.Business.Helper.ModuleFee modulefee, UNITeDataManager dmEnrolment)
    {
        if (modulefee == null) return false;

        UNITe.Database.BusinessClasses.EnrolmentFee enrolmentFee = new UNITe.Database.BusinessClasses.EnrolmentFee(UNITe.InterOp.Globals.Connection);
        UNITe.Business.Helper.EnrolmentFee enrFee = enrolmentFee.CreateEnrolmentFee();
        enrFee.AccountCode = modulefee.AccountCode;
        enrFee.Amount = modulefee.Amount;
        enrFee.CostCentre = modulefee.CostCentre;
        enrFee.Enrolment = dmEnrolment.Read("enrolmenthesa1", "id").ToString();
        enrFee.Currency = modulefee.Currency;
        enrFee.FeeBand = modulefee.FeeBand;
        enrFee.FeeClassification = modulefee.FeeClassification;
        enrFee.HESAIdentifier = modulefee.HESAIdentifier;
        enrFee.InvoiceIdentifier = modulefee.InvoiceIdentifier;
        enrFee.OrganisationStagePaymentPlan = modulefee.OrganisationStagePaymentPlan;
        enrFee.IsOrganisationStagePaymentOverride = modulefee.IsOrganisationStagePaymentOverride;
        enrFee.StudentStagePaymentPlan = modulefee.StudentStagePaymentPlan;


        return enrolmentFee.InsertEnrolmentFee(enrFee);
    }

    private DataTable getModuleDetailsForEnrolment(string route_id)
    {
        //get module id's from route
        //get the database connection
        UNITe.Database.Connection connection = UNITe.InterOp.Globals.Connection;
        //create an instance of the export library - used throughout this script
        UNITe.Database.Report.Export export = new UNITe.Database.Report.Export();

        System.Collections.Generic.List<UNITe.CommonLibrary.SearchForm.SearchFormField> moduleIdForEnrolments = new System.Collections.Generic.List<UNITe.CommonLibrary.SearchForm.SearchFormField>();

        UNITe.CommonLibrary.SearchForm.SearchFormField moduleSearch = new UNITe.CommonLibrary.SearchForm.SearchFormField();
        //set the class/property/value
        moduleSearch.ClassName = "AdmissionsRoute";
        moduleSearch.Property = "id";
        moduleSearch.Value = route_id;
        //add the field to the list
        moduleIdForEnrolments.Add(moduleSearch);

        DataTable dt = export.ExportDataTable(connection, "rg.app.enrrte", moduleIdForEnrolments);


        return dt;
    }

    private List<UNITe.Business.Helper.Unit> getUnitDetailsForAssessments(string module_id)
    {
        UNITe.Database.BusinessClasses.Unit unitQuery = new UNITe.Database.BusinessClasses.Unit(UNITe.InterOp.Globals.Connection);
        //create a parameter object to run query
        UNITe.Business.Helper.UnitParameter unitParam = new UNITe.Business.Helper.UnitParameter();
        //set parameter value - not studentParam has properties for all UNIT - e class properties
        //normal UNIT-e queries such as | and % are supported
        unitParam.Module = module_id;
        unitParam.IsDefaultValue = "-1";
        //since more than one student might be found they are returned in a list
        System.Collections.Generic.List<UNITe.Business.Helper.Unit> units = unitQuery.FindUnit(unitParam);

        return units;
    }

    private bool createAssessmentRecords(string enrId, UNITe.Business.Helper.Unit unit)
    {
        if (unit == null) return false;
        if (string.IsNullOrEmpty(enrId)) return false;

        UNITe.Database.BusinessClasses.Assessment assessment = new UNITe.Database.BusinessClasses.Assessment(UNITe.InterOp.Globals.Connection);
        UNITe.Business.Helper.AssessmentParameter oldAssessParam = new UNITe.Business.Helper.AssessmentParameter();
        oldAssessParam.Enrolment = enrId;
        oldAssessParam.Unit = unit.Id;
        List<UNITe.Business.Helper.Assessment> oldAssessments = assessment.FindAssessment(oldAssessParam);
        if (oldAssessments.Count > 0) return false;

        UNITe.Business.Helper.Assessment newAssessment = assessment.CreateAssessment();
        newAssessment.AssessmentDue = unit.AssessmentsDue;
        newAssessment.Assessor = unit.Assessor;
        newAssessment.AttemptNumber = unit.InitialAttemptNumber;
        newAssessment.Unit = unit.Id;
        newAssessment.Start = unit.Start;
        newAssessment.End = unit.End;
        newAssessment.Reference = unit.Reference;
        newAssessment.Name = unit.Name;
        newAssessment.Type = unit.Type;
        newAssessment.Value = unit.Value;
        newAssessment.NumberElements = unit.NumberElements;
        newAssessment.IsSVQMTransfer = unit.IsSVQMTransfer;
        newAssessment.IsMilestone = unit.IsMilestone;
        newAssessment.Credits = unit.Credits;
        newAssessment.Enrolment = enrId;
        return assessment.InsertAssessment(newAssessment);
    }

}
