using PX.Data;
using PX.Objects.Common;
using PX.Objects.Common.Extensions;
using PX.SM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UploadFileInq_Extension : PXGraphExtension<UploadFileInq>
{
    public override void Initialize()
    {
        base.Initialize();
        DeleteOldFileRevisionsHidden.SetVisible(false);
    }

    public PXAction<FilesFilter> DeleteOldFileRevisions;

    [PXUIField(DisplayName = "Delete Old File Revisions", MapEnableRights = PXCacheRights.Delete, MapViewRights = PXCacheRights.Select)]
    [PXButton]
    protected IEnumerable deleteOldFileRevisions(PXAdapter a)
    {
        if(string.IsNullOrEmpty(base.Base.Filter.Current.DocName))
        {
            throw new PXException("Please enter a value in the 'Document Name' field to filter the files.");
        }

        List<object> list = new List<object>();

        ApplyFilter(Base.Files, list);

        var uploadFiles = Base.Files.Select(list.ToArray()).RowCast<UploadFile>().Select(x => x.FileID);

        if (Base.Files.Ask("Delete Old File Revisions", "Are you sure you want to delete all non current revisions for all visible files in the grid? (You won't be able to undo these changes.)", MessageButtons.YesNo, MessageIcon.Warning) == WebDialogResult.Yes)
        {
            PXLongOperation.StartOperation(Base.UID, delegate
            {
                DoDeleteOldFileRevisions(uploadFiles);
            });
        }

        return a.Get();
    }

    public PXAction<FilesFilter> DeleteOldFileRevisionsHidden;

    [PXUIField(DisplayName = "Delete Old File Revisions (Hidden)", Visibility = PXUIVisibility.Invisible, MapEnableRights = PXCacheRights.Delete, MapViewRights = PXCacheRights.Select)]
    [PXButton]
    protected IEnumerable deleteOldFileRevisionsHidden(PXAdapter a)
    {
        if(string.IsNullOrEmpty(base.Base.Filter.Current.DocName))
        {
            throw new PXException("Please enter a value in the 'Document Name' field to filter the files.");
        }

        List<object> list = new List<object>();

        ApplyFilter(Base.Files, list);

        var uploadFiles = Base.Files.Select(list.ToArray()).RowCast<UploadFile>().Select(x => x.FileID);

        PXLongOperation.StartOperation(Base.UID, delegate
        {
            DoDeleteOldFileRevisions(uploadFiles);
        });

        return a.Get();
    }

    public PXAction<FilesFilter> DeleteFiles;

    [PXUIField(DisplayName = "Delete Files", MapEnableRights = PXCacheRights.Delete, MapViewRights = PXCacheRights.Select)]
    [PXButton]
    protected IEnumerable deleteFiles(PXAdapter a)
    {
        if(string.IsNullOrEmpty(base.Base.Filter.Current.DocName))
        {
            throw new PXException("Please enter a value in the 'Document Name' field to filter the files.");
        }

        List<object> list = new List<object>();

        ApplyFilter(Base.Files, list);

        var uploadFiles = Base.Files.Select(list.ToArray()).RowCast<UploadFile>().Select(x => x.FileID);

        if (Base.Files.Ask("Delete Files", "Are you sure you want to delete all files in the grid? (This could be destructive and You won't be able to undo these changes.)", MessageButtons.YesNo, MessageIcon.Warning) == WebDialogResult.Yes)
        {
            PXLongOperation.StartOperation(Base.UID, delegate
            {
                DoDeleteFiles(uploadFiles);
            });
        }

        return a.Get();
    }

    public static void DoDeleteOldFileRevisions(IEnumerable<Guid?> files)
    {
        PXGraph pXGraph = new PXGraph();
        foreach (PXResult<UploadFile, UploadFileRevisionNoData> item2 in  PXSelectBase<UploadFile, PXSelectJoin<UploadFile, InnerJoin<UploadFileRevisionNoData, On<UploadFile.fileID, Equal<UploadFileRevisionNoData.fileID>, And<UploadFile.lastRevisionID, NotEqual<UploadFileRevisionNoData.fileRevisionID>>>>, Where<UploadFile.fileID, In<Required<UploadFile.fileID>>>>.Config>.Select(pXGraph, files))
        {
            UploadFileRevisionNoData item = item2;
            pXGraph.Caches[typeof(UploadFileRevisionNoData)].Delete(item);
        }

        pXGraph.Caches[typeof(UploadFileRevisionNoData)].Persist(PXDBOperation.Delete);
    }

    public static void DoDeleteFiles(IEnumerable<Guid?> files)
    {
        PXGraph pXGraph = new PXGraph();
        foreach (PXResult<UploadFile> item in  PXSelectBase<UploadFile, PXSelect<UploadFile, Where<UploadFile.fileID, In<Required<UploadFile.fileID>>>>.Config>.Select(pXGraph, files))
        {
            pXGraph.Caches[typeof(UploadFile)].Delete(item);
        }

        pXGraph.Caches[typeof(UploadFile)].Persist(PXDBOperation.Delete);
    }

    private void ApplyFilter(PXSelectBase select, List<object> pars)
    {
        if (Base.Filter.Current.ScreenID == null)
        {
            select.View.Join<LeftJoin<SiteMap, On<SiteMap.screenID, Equal<UploadFile.primaryScreenID>>>>();
            if (Base.Filter.Current.ShowUnassignedFiles == true)
            {
                select.View.WhereAnd<Where<SiteMap.screenID, IsNull>>();
            }
            else
            {
                select.View.WhereAnd<Where<SiteMap.screenID, IsNotNull>>();
            }
        }
        else
        {
            select.View.WhereAnd<Where<UploadFile.primaryScreenID, Equal<Current<FilesFilter.screenID>>>>();
        }

        if (Base.Filter.Current.DocName != null)
        {
            select.View.WhereAnd<Where<UploadFile.name, Like<Required<FilesFilter.docName>>>>();
            string text = select.View.Graph.SqlDialect.PrepareLikeCondition(Base.Filter.Current.DocName);
            pars.Add("%" + text + "%");
        }

        if (Base.Filter.Current.DateCreatedFrom.HasValue)
        {
            select.View.WhereAnd<Where<UploadFile.createdDateTime, GreaterEqual<Current<FilesFilter.dateCreatedFrom>>>>();
        }

        if (Base.Filter.Current.DateCreatedTo.HasValue)
        {
            select.View.WhereAnd<Where<UploadFile.createdDateTime, Less<Required<FilesFilter.dateCreatedTo>>>>();
            pars.Add(Base.Filter.Current.DateCreatedTo.Value.AddDays(1.0));
        }

        if (Base.Filter.Current.AddedBy.HasValue)
        {
            select.View.WhereAnd<Where<UploadFile.createdByID, Equal<Current<FilesFilter.addedBy>>>>();
        }

        if (Base.Filter.Current.CheckedOutBy.HasValue)
        {
            select.View.WhereAnd<Where<UploadFile.checkedOutBy, Equal<Current<FilesFilter.checkedOutBy>>>>();
        }
    }

    
}