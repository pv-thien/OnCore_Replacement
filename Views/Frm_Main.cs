﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComponentFactory.Krypton.Toolkit;
using Ingr.SP3D.Common.Client;
using Ingr.SP3D.Common.Client.Services;
using Ingr.SP3D.Common.Middle;
using Ingr.SP3D.Common.Middle.Services;
using Ingr.SP3D.Common.Middle.Services.Hidden;
using Ingr.SP3D.Route.Middle;
using OnCore_Replacement.DataSQL;
using ROUTEENTITIESLib;

namespace OnCore_Replacement.Views
{
	public partial class Frm_Main : KryptonForm
	{
		public Frm_Main()
		{
			InitializeComponent();
		}

		//
		// Summary:
		//    Load data for replacement
		private void btnImportData_Click(object sender, EventArgs e)
		{
			LoadDataReplace();
			MessageBox.Show("Load Data success", "OnCore : Load Data");
		}

		//
		// Summary:B
		//    Fuc replacing
		private void btnReplaceObj_Click(object sender, EventArgs e)
		{
			//LoadDataReplaceCompare();
			TransactionManager oTransactionManager = MiddleServiceProvider.TransactionMgr;
			SP3DConnection oSP3DConnection = ClientServiceProvider.WorkingSet.ActiveConnection;
			PlantConnection oPlantConnection = oSP3DConnection as PlantConnection;
			List<DataStorageReplace> oListOBJReplaced = new List<DataStorageReplace>();
			List<object> oListOBJCompare = new List<object>();
			int indexRow = -1;
			if (dgvDataReplaceCompare.Rows.Count > 0)
			{
				oListOBJCompare = GetUniqueValues(dgvDataReplaceCompare, "tbfeatOid2");
			}	
			try
			{
				List<RouteFeature> oBOPipeEndFeat = new List<RouteFeature>();
				BOCollection oBOBusinessObject = new BOCollection();
				PipeRun oPipeRun = default;
				Position oPosition = default;
				List<List<DataNote>> oBONote = new List<List<DataNote>>();

				if (dgvDataReplacement.Rows.Count > 0 ) 
				{
					for (int i = 0; i < dgvDataReplacement.Rows.Count; i++) 
					{
						//string sOID = "{" + dgvDataReplacement.Rows[i].Cells["tbfeatOid1"].Value.ToString() + "}";
						GetObjByOID(dgvDataReplacement.Rows[i].Cells["tbfeatOid1"].Value.ToString(), out BusinessObject oBo);
						GetObjByOID(dgvDataReplacement.Rows[i].Cells["tbpartOid1"].Value.ToString(), out BusinessObject oBoPart);
						if (oBo != null && oBo is RouteFeature) 
						{
							oBOPipeEndFeat.Add((RouteFeature)oBo);
						}
						if (oBoPart != null && oBoPart is RoutePart)
						{
							List<DataNote> oListNote = new List<DataNote>();
							
							foreach (Note note in oBoPart.Notes)
							{
								DataNote dataNote = new DataNote
								{
									NoteText = note.Text,
									NoteName = note.Name,
									NotePurpose = note.Purpose,
									PartName = dgvDataReplacement.Rows[i].Cells["tbpartName1"].Value.ToString(),
									RunOid = dgvDataReplacement.Rows[i].Cells["tbrunOid1"].Value.ToString(),
									RunName = dgvDataReplacement.Rows[i].Cells["tbrunName1"].Value.ToString(),
									LineOid = dgvDataReplacement.Rows[i].Cells["tblineOid1"].Value.ToString(),
									LineName = dgvDataReplacement.Rows[i].Cells["tblineName1"].Value.ToString()
								};
								oListNote.Add(dataNote);
							}
							oBONote.Add(oListNote);
						}	
					}
				}

				if (oBONote != null)
				{ ExportToLogFile(oBONote); }

				if (oBOPipeEndFeat != null )
				{
					for (int i = 0; i< oBOPipeEndFeat.Count; i++)
					{
						indexRow = i;
						//if (checkboxHeader.Checked == false) 
						//{

						//}
						bool isChecked = Convert.ToBoolean(dgvDataReplacement.Rows[i].Cells["tbselectObj1"].Value);

						if (dgvDataReplacement.Rows[i].Cells["tbtagAvailable1"].Value.ToString() != "No" && isChecked)
						{
							DataStorageReplace dataStorageReplace = new DataStorageReplace();

							oPipeRun = (PipeRun)oBOPipeEndFeat[i].Run;
							oPosition = oBOPipeEndFeat[i].Location;
							OrientationAngleAlongLegFeature(oBOPipeEndFeat[i], default, out	double dOriginalAngle);
							dataStorageReplace.OldFeatOID = oBOPipeEndFeat[i].ObjectID;

							oBOPipeEndFeat[i].Delete();
							oTransactionManager.Commit("");

							oPipeRun.GetFeatureAtLocation(Ingr.SP3D.Route.Middle.PathFeatureObjectTypes.PathFeatureType_STRAIGHT, Ingr.SP3D.Route.Middle.PathFeatureFunctions.PathFeatureFunction_ROUTE, oPosition, out Position _, out RouteFeature oRouteFeat);
							oPipeRun.InsertFeatureByTagOnFeature(dgvDataReplacement.Rows[i].Cells["tbtagAvailable1"].Value.ToString(), "", oRouteFeat, oPosition, oPipeRun, out RouteFeature oInsertedFeature);
							
							OrientationAngleAlongLegFeature(oInsertedFeature, dOriginalAngle, out double _);
							oTransactionManager.Commit("");
							GetRoutePartFromOIDRoutFeatAndAddName(oInsertedFeature.ObjectID, dgvDataReplacement.Rows[i].Cells["tbtagAvailable1"].Value.ToString());
							oTransactionManager.Commit("");

							if (oBONote[i].Count > 0)
							{
								GetRoutePartFromOIDRoutFeatAndAddNote(oInsertedFeature.ObjectID, oBONote[i]);
								oTransactionManager.Commit("");
							}

							dataStorageReplace.NewFeatOID = oInsertedFeature.ObjectID;
							dataStorageReplace.ObjTagReplace = dgvDataReplacement.Rows[i].Cells["tbtagAvailable1"].Value.ToString();
							oListOBJReplaced.Add(dataStorageReplace);

						}
					}
				}

				LoadDataReplace();
				checkboxHeader.Checked = false;

				if (oListOBJCompare != null && oListOBJReplaced != null)
				{
					foreach (var item in oListOBJReplaced)
					{
						//int indexInListCompare = oListOBJCompare.IndexOf(item.OldFeatOID.ToLower());
						//if (indexInListCompare != -1)
						//{
						//	dgvDataReplaceCompare.Rows[indexInListCompare].DefaultCellStyle.BackColor = Color.FromArgb(102, 204, 255);
						//	dgvDataReplaceCompare.Rows[indexInListCompare].Cells[8].Value = item.NewFeatOID.ToString();

						//	GetObjByOID(item.NewFeatOID, out BusinessObject oBo);
						//	HiliterObject(oBo);
						//}	
						foreach (DataGridViewRow row in dgvDataReplacement.Rows)
						{
							string _ = item.NewFeatOID.Replace("{", "");
							string newFeatOid = _.Replace("}", "");
							int index = row.Cells["tbfeatOid1"].Value.ToString().IndexOf(newFeatOid.ToLower());
							if (index != -1)
							{
								row.Cells["tbStatus"].Value = "Success";
								row.DefaultCellStyle.BackColor = default;
								GetObjByOID(item.NewFeatOID, out BusinessObject oBo);
								HiliterObject(oBo);
							}
						}
					}

				}
			}
			catch (Exception ex)
			{
				if (indexRow != -1)
				{
					dgvDataReplacement.Rows[indexRow].DefaultCellStyle.BackColor = Color.Red;
					dgvDataReplacement.Rows[indexRow].Cells["tbStatus"].Value = "Fail";
				}
				MessageBox.Show(ex.Message, "OnCore : Err replace obj");
				//oTransactionManager.Undo();
			}
			finally
			{
				//oTransactionManager.Commit("");
				//LoadDataReplace();
				//checkboxHeader.Checked = false;
				
				//if (oListOBJCompare != null && oListOBJReplaced != null )
				//{
				//	foreach (var item in oListOBJReplaced)
				//	{
				//		//int indexInListCompare = oListOBJCompare.IndexOf(item.OldFeatOID.ToLower());
				//		//if (indexInListCompare != -1)
				//		//{
				//		//	dgvDataReplaceCompare.Rows[indexInListCompare].DefaultCellStyle.BackColor = Color.FromArgb(102, 204, 255);
				//		//	dgvDataReplaceCompare.Rows[indexInListCompare].Cells[8].Value = item.NewFeatOID.ToString();

				//		//	GetObjByOID(item.NewFeatOID, out BusinessObject oBo);
				//		//	HiliterObject(oBo);
				//		//}	
				//		foreach(DataGridViewRow row in dgvDataReplacement.Rows)
				//		{
				//			string _ = item.NewFeatOID.Replace("{", "");
				//			string newFeatOid = _.Replace("}", "");
				//			int index = row.Cells["tbfeatOid1"].Value.ToString().IndexOf(newFeatOid.ToLower());
				//			if (index != -1)
				//			{
				//				row.Cells["tbStatus"].Value = "Success";
				//				row.DefaultCellStyle.BackColor = default;
				//				GetObjByOID(item.NewFeatOID, out BusinessObject oBo);
				//				HiliterObject(oBo);
				//			}
				//		}
				//	}
					
				//}
				MessageBox.Show("Replace finish", "OnCore : Replacement");
			}

		}

		//
		// Summary:
		//    Hilight object replace fall
		private void HilightObjectReplaceFall()
		{

		}

		//
		// Summary:
		//    Load data namr server and name DB
		private void Frm_Main_Load(object sender, EventArgs e)
		{
			SP3DConnection oSP3DConnection = ClientServiceProvider.WorkingSet.ActiveConnection;
			PlantConnection oPlantConnection = oSP3DConnection as PlantConnection;

			AllData.NameServer = oSP3DConnection.Server;
			AllData.DbName = oPlantConnection.ParentPlant.ToString() + "_RDB";
		}

		//
		// Summary:
		//    Modify angle 
		public void OrientationAngleAlongLegFeature(RouteFeature oRFE, double dAngle, out double dOriginalAngle)
		{
			dOriginalAngle = default;
			AlongLegFeature alongLegFeature = oRFE as AlongLegFeature;
			if (alongLegFeature != null)
			{
				if (dAngle != default)
				{
					alongLegFeature.OrientationAngle = dAngle;
				}
				else
				{
					dOriginalAngle = alongLegFeature.OrientationAngle;
				}
					
			}
		}

		//
		// Summary:
		//    Load data from server(Sql server) to replace
		public void LoadDataReplace()
		{
			try
			{
				if(cbSelectOptionLoadData.SelectedItem.ToString() == "Data Base")
				{
					ConnectSQL connectSQL = new ConnectSQL("Data Source=" + AllData.NameServer + ";Initial Catalog=" + AllData.DbName + ";Integrated Security=True");
					if (connectSQL.IsConnection)
					{
						DataTable dt = DataProvider.Instance.LoadDataReplacement();
						if (dt != null)
						{
							if (dgvDataReplacement.Rows.Count > 0)
							{
								dgvDataReplacement.DataSource = null;
								dgvDataReplacement.Rows.Clear();
								dgvDataReplacement.Refresh();
							}
							for (int i = 0; i < dt.Rows.Count; i++)
							{
								dgvDataReplacement.Rows.Add(0, "---", dt.Rows[i][0], dt.Rows[i][1], dt.Rows[i][2], dt.Rows[i][3], dt.Rows[i][4], dt.Rows[i][5], dt.Rows[i][6], dt.Rows[i][7], dt.Rows[i][8], dt.Rows[i][9]);

							}
						}
						CreateCheckBoxHeader();
						checkboxHeader.Click += new System.EventHandler(this.checkboxSelectAll_Click);
						DisableCellDataGridView();
					}
				}
				else if (cbSelectOptionLoadData.SelectedItem.ToString() == "Section")
				{
					Filter oFilter = new Filter();
					oFilter.Definition.AddObjectType("Piping\\PipingFeatures");
					BOCollection oWorkingcoll = ClientServiceProvider.WorkingSet.GetObjectsByFilter(oFilter, ClientServiceProvider.WorkingSet.ActiveConnection);
					
					List<DataObjectByLoadSection> dataObjectByLoadSectionList = new List<DataObjectByLoadSection>();

					foreach (BusinessObject businessObject in oWorkingcoll)
					{
						DataObjectByLoadSection dataObject = new DataObjectByLoadSection();
						AlongLegFeature alongLegFeature = businessObject as AlongLegFeature;
						if (alongLegFeature != null)
						{
							RouteFeature routeFeature = alongLegFeature as RouteFeature;
							IJRtePipePathFeat pipePathFeat = (IJRtePipePathFeat)COMConverters.ConvertBOToCOMBO(alongLegFeature);
							
							foreach(RoutePart part in routeFeature.Parts)
							{
								Type type = part.GetType();
								if (type.Name == "PipeInstrument" || type.Name == "PipeSpecialty")
								{	
									dataObject.PartOID = part.ObjectID; 
									dataObject.PartName = part.Name;
									dataObject.OBJType = type.Name;
									dataObject.FeatOID = routeFeature.ObjectID;
									dataObject.Tag = pipePathFeat.Tag;
									dataObject.TagAvailable = DataProvider.Instance.CheckTagAvailable(dataObject.PartName);
									dataObject.RunOID = routeFeature.Run.ObjectID;
									dataObject.RunName = routeFeature.Run.Name;
									BusinessObject pipeLine = (BusinessObject)routeFeature.Run.SystemParent;
									dataObject.LineOID = pipeLine.ObjectID;
									dataObject.LineName = routeFeature.Run.SystemParent.ToString();

									dataObjectByLoadSectionList.Add(dataObject);
								}
							}
						}
					}

					if (dataObjectByLoadSectionList != null)
					{
						if (dgvDataReplacement.Rows.Count > 0)
						{
							dgvDataReplacement.DataSource = null;
							dgvDataReplacement.Rows.Clear();
							dgvDataReplacement.Refresh();
						}
						for (int i = 0; i < dataObjectByLoadSectionList.Count; i++)
						{
							dgvDataReplacement.Rows.Add(0, "---", dataObjectByLoadSectionList[i].PartOID, dataObjectByLoadSectionList[i].PartName, dataObjectByLoadSectionList[i].OBJType, dataObjectByLoadSectionList[i].FeatOID
								,dataObjectByLoadSectionList[i].Tag, dataObjectByLoadSectionList[i].TagAvailable, dataObjectByLoadSectionList[i].RunOID, dataObjectByLoadSectionList[i].RunName, dataObjectByLoadSectionList[i].LineOID, dataObjectByLoadSectionList[i].LineName);

						}
					}
					CreateCheckBoxHeader();
					checkboxHeader.Click += new System.EventHandler(this.checkboxSelectAll_Click);
					DisableCellDataGridView();

				}
				else 
				{
					MessageBox.Show("Please select type load data", "OnCore : Err load data replace");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "OnCore : Err load data replace");
			}
		}

		//
		// Summary:
		//    Create check box in header datagridview
		private KryptonCheckBox checkboxHeader = new KryptonCheckBox();
		private void CreateCheckBoxHeader()
		{
			Point hederlocation = dgvDataReplacement.GetCellDisplayRectangle(0, -1, true).Location;
			checkboxHeader.Location = new Point(hederlocation.X + 17, hederlocation.Y + 6);
			checkboxHeader.Size = new Size(18, 18);
			checkboxHeader.BackColor = Color.White;
			checkboxHeader.Text = null;
			dgvDataReplacement.Controls.Add(checkboxHeader);
		}
		private void checkboxSelectAll_Click(object sender, EventArgs e)
		{
			dgvDataReplacement.EndEdit();

			foreach (DataGridViewRow row in dgvDataReplacement.Rows)
			{
				DataGridViewCheckBoxCell checkBoxCell = (DataGridViewCheckBoxCell)row.Cells[0];
				if (row.Cells["tbtagAvailable1"].Value.ToString() != "No" && checkboxHeader.Checked == true && row.Cells["tbTag1"].Value.ToString() != row.Cells["tbtagAvailable1"].Value.ToString())
				{
					checkBoxCell.Value = checkboxHeader.Checked;
				}
				if (checkboxHeader.Checked == false)
				{
					checkBoxCell.Value = checkboxHeader.Checked;
				}
			}
		}

		//
		// Summary:
		//    Load data from server(Sql server) to compare after replacing
		public void LoadDataReplaceCompare()
		{
			try
			{
				ConnectSQL connectSQL = new ConnectSQL("Data Source=" + AllData.NameServer + ";Initial Catalog=" + AllData.DbName + ";Integrated Security=True");
				if (connectSQL.IsConnection)
				{
					DataTable dt = DataProvider.Instance.LoadDataBeforeReplacement();
					if (dt != null)
					{
						if (dgvDataReplaceCompare.Rows.Count > 0)
						{
							dgvDataReplaceCompare.DataSource = null;
							dgvDataReplaceCompare.Rows.Clear();
							dgvDataReplaceCompare.Refresh();
						}
						for (int i = 0; i < dt.Rows.Count; i++)
						{
							dgvDataReplaceCompare.Rows.Add(dt.Rows[i].ItemArray);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "OnCore : Err load data repalce compare");
			}
		}

		//
		// Summary:
		//    Get data from Datagridview
		private List<object> GetUniqueValues(DataGridView dataGridView, string columnName)
		{
			List<object> uniqueValues = dataGridView.Rows.Cast<DataGridViewRow>()
						.Where(row => row.Cells[columnName].Value != null)
						.Select(row => row.Cells[columnName].Value)
						.Distinct()
						.ToList();

			for (int i = 0; i < uniqueValues.Count; i++)
			{
				uniqueValues[i] = $"{{{uniqueValues[i]}}}";
			}

			return uniqueValues;
		}

		//
		// Summary:
		//    Get route part from OID route feat and add notes, name for route part
		private void GetRoutePartFromOIDRoutFeatAndAddName(string sOIDRouteFeat, string sName)
		{
			BusinessObject oBo = null;
			if (sOIDRouteFeat.Contains("}"))
			{
				GetObjByOID(sOIDRouteFeat, out oBo);
			}
			else
			{
				GetObjByOID("{" + sOIDRouteFeat + "}", out oBo);
			}
			if (oBo is RouteFeature)
			{
				try
				{
					DataTable dt = DataProvider.Instance.LoadDataOIDObject();
					if (dt != null)
					{
						string targetValueOID = Regex.Replace(oBo.ObjectID.ToLower(), "[{}]", "");

						var query = from row in dt.AsEnumerable()
									let columnIndex = dt.Columns["tbfeatOid"].Ordinal
									let columnValue = row[columnIndex]?.ToString()
									where columnValue == targetValueOID
									select new { RowIndex = dt.Rows.IndexOf(row), Value = targetValueOID };

						var result = query.FirstOrDefault();

						if (result != null)
						{
							string sOIDRoutePart = dt.Rows[result.RowIndex][0].ToString();
							GetObjByOID("{" + sOIDRoutePart + "}", out oBo);
							if (oBo is RoutePart)
							{
								RoutePart oRoutePart = (RoutePart)oBo;
								if (oRoutePart != null)
								{
									oRoutePart.SetUserDefinedName(sName);
								}

							}
						}
					}

				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "OnCore : Err find and add Note into Tag");
				}

			}

		}
		private void GetRoutePartFromOIDRoutFeatAndAddNote(string  sOIDRouteFeat, List<DataNote> oBONote)
		{
			BusinessObject oBo = null;
			if (sOIDRouteFeat.Contains("}"))
			{
				GetObjByOID(sOIDRouteFeat, out oBo);
			}
			else
			{
				GetObjByOID("{" + sOIDRouteFeat + "}", out oBo);
			}
			if (oBo is RouteFeature)
			{
				try
				{
					DataTable dt = DataProvider.Instance.LoadDataOIDObject();
					if (dt != null)
					{
						string targetValueOID = Regex.Replace(oBo.ObjectID.ToLower(), "[{}]", "");

						var query = from row in dt.AsEnumerable()
									let columnIndex = dt.Columns["tbfeatOid"].Ordinal
									let columnValue = row[columnIndex]?.ToString()
									where columnValue == targetValueOID
									select new { RowIndex = dt.Rows.IndexOf(row), Value = targetValueOID };

						var result = query.FirstOrDefault();

						if (result!= null)
						{
							string sOIDRoutePart = dt.Rows[result.RowIndex][0].ToString();
							GetObjByOID("{" + sOIDRoutePart + "}", out oBo);
							if (oBo is RoutePart)
							{
								RoutePart oRoutePart = (RoutePart)oBo;
								foreach (DataNote dataNote in oBONote) 
								{
									Note oNote = new Note(oRoutePart);
									Ingr.SP3D.Common.Middle.Services.Model oModel = MiddleServiceProvider.SiteMgr.ActiveSite.ActivePlant.PlantModel;
									CodelistItem oCLI = default;
									switch (dataNote.NotePurpose)
									{
										case 0:
											break;
										case 1:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.General));
											break;
										case 2:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.Design));
											break;
										case 3:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.Fabrication));
											break;
										case 4:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.Installation));
											break;
										case 5:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.OperationandMaintenance));
											break;
										case 6:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.Inspection));
											break;
										case 7:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.Remark));
											break;
										case 8:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.MaterialofConstruction));
											break;
										case 9:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.DesignReview));
											break;
										case 10:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.PipingSpecificationnote));
											break;
										case 11:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.Justification));
											break;
										case 12:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.Procurement));
											break;
										case 13:
											oCLI = oModel.MetadataMgr.GetCodelistInfo("NotePurpose", "CMNSCH").GetCodelistItem(DataNotePurposeHelper.GetDescription(DataNotePurpose.Standardnote));
											break;
									}
									
									if (oCLI != default)
									{ oNote.SetPropertyValue(oCLI, "IJGeneralNote", "Purpose"); }
									
									oNote.SetPropertyValue(dataNote.NoteText, "IJGeneralNote", "Text");
									oNote.SetPropertyValue(dataNote.NoteName, "IJGeneralNote", "Name");
								}
							}	
						}
					}
					
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "OnCore : Err find and add Note into Tag");
				}

			}

		}

		//
		// Summary:
		//    Block function close page
		private void kryptonDockableNavigator1_CloseAction(object sender, ComponentFactory.Krypton.Navigator.CloseActionEventArgs e)
		{
			e.Action = ComponentFactory.Krypton.Navigator.CloseButtonAction.None;
		}

		//
		// Summary:
		//    Fuc Hilight object
		private GraphicViewHiliter _oHiliter;
		public void HiliterObject(BusinessObject businessObject)
		{
			 _oHiliter = new GraphicViewHiliter();
			((HiliterBase)_oHiliter).Weight = 1f;
			((HiliterBase)_oHiliter).Color = 65535;
			HybridBOCollection hilitedObjects = _oHiliter.HilitedObjects;
			((ReadWriteBOCollectionBase)hilitedObjects).Add(businessObject);
			((IDisposable)hilitedObjects)?.Dispose();
		}
		public void ClearHighlightElements()
		{
			try
			{
				if (_oHiliter != null)
				{
					HybridBOCollection val = null;
					HybridBOCollection val2 = (val = _oHiliter.HilitedObjects);
					try
					{
						((ReadWriteBOCollectionBase)val).Clear();
						return;
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "OnCore : Err clear hilight  object");
			}
		}

		//
		// Summary:
		//    Get object from the OID
		private void GetObjByOID(string sOidOld, out BusinessObject oBo)
		{
			string sOid = sOidOld;
			if (!sOid.Contains("}"))
			{
				sOid = "{" + sOidOld + "}";
			}
			SP3DConnection oSP3DConnection = ClientServiceProvider.WorkingSet.ActiveConnection;
			oBo = null;
			try
			{
				if (sOid != null)
				{
					BOMoniker oBOMoniker = oSP3DConnection.GetBOMonikerFromDbIdentifier(sOid);
					oBo = oSP3DConnection.WrapSP3DBO(oBOMoniker);
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message, "OnCore : Err get object by OID");
			}
		}

		private void btnExportData_Click(object sender, EventArgs e)
		{
			
			

		}

		//
		// Summary:
		//    Fuc Hilight object when double click on data grid view 
		private void dgvDataReplacement_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			try
			{
				if (dgvDataReplacement.Rows[e.RowIndex].Cells[e.ColumnIndex] != null && e.ColumnIndex != 0 && e.ColumnIndex != 1 && e.ColumnIndex != 3 && e.ColumnIndex != 4
					&& e.ColumnIndex != 6 && e.ColumnIndex != 7 && e.ColumnIndex != 9 && e.ColumnIndex != 11)
				{
					if (dgvDataReplacement.Rows[e.RowIndex].DefaultCellStyle.BackColor != Color.Red)
					{
						ClearHighlightElements();
						string sOID = dgvDataReplacement.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
						SelectObjectWhenDoubleClickDGV(sOID);
					}	
					else
					{
						if (e.ColumnIndex != 5 && e.ColumnIndex != 2)
						{
							ClearHighlightElements();
							string sOID = dgvDataReplacement.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
							SelectObjectWhenDoubleClickDGV(sOID);
						}	
					}	
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "OnCore : Err hilight object by OID or object does not exist");
			}
		}

		//
		// Summary:
		//    Fuc disable cell  
		public void DisableCellDataGridView() 
		{
			foreach (DataGridViewRow row in dgvDataReplacement.Rows)
			{
				if (row.Cells["tbtagAvailable1"].Value.ToString() == "No" || row.Cells["tbTag1"].Value.ToString() == row.Cells["tbtagAvailable1"].Value.ToString())
				{
					DataGridViewCell cell = row.Cells[0];
					DataGridViewCheckBoxCell chkCell = cell as DataGridViewCheckBoxCell;
					chkCell.Value = false;
					chkCell.FlatStyle = FlatStyle.Flat;
					chkCell.Style.ForeColor = Color.DarkGray;
					cell.ReadOnly = true;
					row.DefaultCellStyle.BackColor = Color.FromArgb(185, 186, 189);
				}
			}	
		}

		//
		// Summary:
		//    Fuc Hilight object when double click on data grid view 
		private void dgvDataReplaceCompare_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			try
			{
				if (dgvDataReplaceCompare.Rows[e.RowIndex].Cells[e.ColumnIndex] != null && e.ColumnIndex != 1 && e.ColumnIndex != 2 && e.ColumnIndex != 4 && e.ColumnIndex != 5 && e.ColumnIndex != 6
					&& e.ColumnIndex != 7 && e.ColumnIndex != 10 && e.ColumnIndex != 12)
				{
					ClearHighlightElements();
					string sOID = dgvDataReplaceCompare.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();

					if (dgvDataReplaceCompare.Rows[e.RowIndex].DefaultCellStyle.BackColor == Color.FromArgb(102, 204, 255))
					{
						if (e.ColumnIndex != 0 && e.ColumnIndex != 3)
						{
							HilightObjectWhenDoubleClickinDGV(sOID);
						}	
					}
					else
					{
						HilightObjectWhenDoubleClickinDGV(sOID);
					}	
					
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "OnCore : Err hilight object by OID or object does not exist");
			}
		}

		private void HilightObjectWhenDoubleClickinDGV( string sOID)
		{
			if (sOID.Contains("}"))
			{
				GetObjByOID(sOID, out BusinessObject oBo);
				HiliterObject(oBo);
			}
			else
			{
				GetObjByOID("{" + sOID + "}", out BusinessObject oBo);
				HiliterObject(oBo);
			}
		}

		private void SelectObjectWhenDoubleClickDGV( string sOID)
		{
			if (ClientServiceProvider.SelectSet.SelectedObjects.Count > 0) 
			{
				ClientServiceProvider.SelectSet.SelectedObjects.Clear();
			}
			if (sOID.Contains("}"))
			{
				GetObjByOID(sOID, out BusinessObject oBo);
				ClientServiceProvider.SelectSet.SelectedObjects.Add(oBo);
			}
			else
			{
				GetObjByOID("{" + sOID + "}", out BusinessObject oBo);
				ClientServiceProvider.SelectSet.SelectedObjects.Add(oBo);
			}
		}

		//
		// Summary:
		//    Fuc Export file log Note
		private void ExportToLogFile(List<List<DataNote>> dataList)
		{
			string filePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\LogDataNote.txt";
			using (StreamWriter sw = new StreamWriter(filePath))
			{
				foreach (var data in dataList)
				{
					foreach(var item in data) 
					{
						if (item.NoteName != null) 
						{
							foreach (var property in typeof(DataNote).GetProperties())
							{
								sw.WriteLine($"{property.Name}: {property.GetValue(item)}");
							}
							sw.WriteLine();
						}
					}
				}
			}
		}

		//
		// Summary:
		//    Fuc update value progree bar
		private void UpdateProgressBar(int i)
		{
			//this.Invoke((MethodInvoker)delegate {
			//	//circularProgressBar1.Visible = true;
			//	//circularProgressBar1.Text = "Please Wait ....";
			//	//circularProgressBar1.Value = i;
			//});
		}
	}
}
