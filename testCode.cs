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

    private UNITeDataManager courseDM;
    private UNITeDataManager courseYearDM;
    private UNITeDataManager moduleDM;
    public CodeBehindClass(Control control)
    {
        //A quick and dirty way to get when the form is displayed. 
        control.VisibleChanged += new EventHandler(control_VisibleChanged);
    }

    private void control_VisibleChanged(object sender, EventArgs e)
    {
        Control control = sender as Control;
        if (control.Visible)
        {
            control.VisibleChanged -= new EventHandler(control_VisibleChanged);
            this.mainForm = control.FindForm();

            foreach (UNITeDataManager dm in UNITeDataManager.DataManagers)
            {
                if (dm.Name == "Course")
                {
                    courseDM = dm;
                }
                if (dm.Name == "CourseYear")
                {
                    courseYearDM = dm;
                }
                if (dm.Name == "Module")
                {
                    moduleDM = dm;
                }
            }
            courseDM.OnCurrentRecordChange += CourseDM_OnCurrentRecordChange;
            courseYearDM.OnCurrentRecordChange += CourseYearDM_OnCurrentRecordChange;
            moduleDM.OnCurrentRecordChange += ModuleDM_OnCurrentRecordChange;


        }
        
    }

    private void CourseDM_OnCurrentRecordChange(UNITe.InformationInterface.Scripting.DataManagerArguments args)
    {
        if (args.Alias == "modulehesa1")
        {
            clearChildDatamanager("CourseYear");
            clearChildDatamanager("Module");
            string id = courseDM.Read("modulehesa1", "id").ToString();
            if (!string.IsNullOrEmpty(id))
            {
                populateChildDatamanger(id, "CourseYear");
            }
        }
    }

    private void CourseYearDM_OnCurrentRecordChange(UNITe.InformationInterface.Scripting.DataManagerArguments args)
    {
        if (args.Alias == "modulehesa1")
        {
            clearChildDatamanager("Module");
            string id = courseYearDM.Read("modulehesa1", "id").ToString();
            if (!string.IsNullOrEmpty(id))
            {
                populateChildDatamanger(id, "Module");
            }


            if (courseDM.Read("modulehesa1", "id") == null)
            {
                populateParentDatamanger(id, "Course");
            }

        }
    }
    private void ModuleDM_OnCurrentRecordChange(UNITe.InformationInterface.Scripting.DataManagerArguments args)
    {
        if (args.Alias == "modulehesa1")
        {
            string id = courseYearDM.Read("modulehesa1", "id").ToString();
            
            if (courseYearDM.Read("modulehesa1", "id") == null)
            {
                clearChildDatamanager("CourseYear");
                populateParentDatamanger(id, "CourseYear");
            }

        }
    }

    private void populateChildDatamanger(string id, string dataManagerName)
    {
        List<UNITe.Business.Helper.Offering> offerings = getOfferingBySource(id);

        string strOfferingIds = string.Empty;
        foreach (UNITe.Business.Helper.Offering offering in offerings)
        {
            if (strOfferingIds.Length > 0 && offering.Destination.Length > 0)
            {
                strOfferingIds = strOfferingIds + "|" + offering.Destination;
            }
            else strOfferingIds = offering.Destination;
        }

        if (strOfferingIds.Length < 5) return;  //not a correct id - 16 digits expected

        foreach (UNITeDataManager dm in UNITeDataManager.DataManagers)
        {
            // find the data manager you want to search
            if (dm.Name == dataManagerName)
            {
                //set parameter using alias and property reference.
                //Parameter table has 1 row of index 0.
                dm.DataSetParam.Tables["modulehesa1"].Rows[0]["id"] = strOfferingIds;
                //call search
                dm.Search();

            }
        }
    }


    private void populateParentDatamanger(string id, string dataManagerName)
    {
        List<UNITe.Business.Helper.Offering> offerings = getOfferingByDestination(id);

        string strOfferingIds = string.Empty;
        foreach (UNITe.Business.Helper.Offering offering in offerings)
        {
            if (strOfferingIds.Length > 0 && offering.Source.Length > 0)
            {
                strOfferingIds = strOfferingIds + "|" + offering.Source;
            }
            else strOfferingIds = offering.Source;
        }

        if (strOfferingIds.Length < 5) return;  //not a correct id - 16 digits expected

        foreach (UNITeDataManager dm in UNITeDataManager.DataManagers)
        {
            // find the data manager you want to search
            if (dm.Name == dataManagerName)
            {
                //set parameter using alias and property reference.
                //Parameter table has 1 row of index 0.
                dm.DataSetParam.Tables["modulehesa1"].Rows[0]["id"] = strOfferingIds;
                //call search
                dm.Search();

            }
        }
    }

    private void clearChildDatamanager(string dataManagerName)
    {
        foreach (UNITeDataManager dm in UNITeDataManager.DataManagers)
        {
            // find the data manager you want to search
            if (dm.Name == dataManagerName)
            {
                dm.Clear();
            }
        }
    }

    private List<UNITe.Business.Helper.Offering> getOfferingBySource(string id)
    {
        UNITe.Database.BusinessClasses.Offering offeringQuery = new UNITe.Database.BusinessClasses.Offering(UNITe.InterOp.Globals.Connection);
        //create a parameter object to run query
        UNITe.Business.Helper.OfferingParameter offeringParam = new UNITe.Business.Helper.OfferingParameter();
        offeringParam.Source = id;
        System.Collections.Generic.List<UNITe.Business.Helper.Offering> offerings = offeringQuery.FindOffering(offeringParam);

        return offerings;
    }

    private List<UNITe.Business.Helper.Offering> getOfferingByDestination(string id)
    {
        UNITe.Database.BusinessClasses.Offering offeringQuery = new UNITe.Database.BusinessClasses.Offering(UNITe.InterOp.Globals.Connection);
        //create a parameter object to run query
        UNITe.Business.Helper.OfferingParameter offeringParam = new UNITe.Business.Helper.OfferingParameter();
        offeringParam.Destination = id;
        System.Collections.Generic.List<UNITe.Business.Helper.Offering> offerings = offeringQuery.FindOffering(offeringParam);

        return offerings;
    }
}
