using Application.DataTransferObjects.Transactions.Packing.VendorReturnAuthorization;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using Database.Libraries.Repositories;
using Integration.NS.DataTransferObjects.Packing.Returns;
using Integration.NS.DataTransferObjects.Packing.STR;
using Integration.NS.DataTransferObjects.Packing.VendorReturnAuthorization;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Shared.Entities;
using Shared.Libraries.Utilities;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

namespace Integration.NS.Implementations.Transactions.Packing;

internal class VendorReturnAuthorizationPackingIntegration(
    INetSuiteApiClientService netsuiteService,
    ISqlQueryManager sqlQuery,
    IHttpContextAccessor httpContextAccessor,
    SuiteQLQueryBuilderFactoryService builderFactory) : IVendorReturnAuthorizationPackingIntegration
{
    public async Task<(IEnumerable<VendorReturnAuthorizationDataGridDTO> Data, int Count)> GetPackingVendorReturnAuthorizationsList(DataGridIntent intent, int subsidiaryId)
    {

        if (intent.Sorts.Count == 0) intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(VendorReturnAuthorizationPackingDataGridNSDTO.DateLastModified)));
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.Id)),
                ("t.tranid", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.ReferenceNumber)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.Date)),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.DateLastModified)),
                ("BUILTIN.DF(t.subsidiary)", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.SourceSubsidiary)),
                ("BUILTIN.DF(t.tosubsidiary)", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.DestinationSubsidiary)),
                ("BUILTIN.DF(tl.location)", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.Location)),
                ("BUILTIN.DF(t.transferlocation)", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.TransferLocation)),
                ("BUILTIN.DF(t.status)", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.Status)),
                ("t.memo", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.Remarks))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .Join("entity e", on: "t.entity = e.id")
            .WithFilters(
                Equal("tl.mainline", "T"),
                Equal("t.subsidiary", subsidiaryId))
            .WithFilters(PackingVendorReturnAuthorizationFilters())
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<VendorReturnAuthorizationPackingDataGridNSDTO>(netsuiteService);

        return (response.items.Select(MapDataGridDto), response.totalResults);
    }

    public async Task<VendorReturnAuthorizationInfoDTO?> GetPackingVendorReturnAuthorization(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.Id)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.Date)),
                ("t.tranid", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.ReferenceNumber)),
                ("BUILTIN.DF(t.subsidiary)", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.FromSubsidiary)),
                ("BUILTIN.DF(tl.location)", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.Location)),
                ("BUILTIN.DF(t.transferlocation)", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.TransferLocation)),
                ("CONCAT(em.firstname,CONCAT(' ',em.lastname))", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.PreparedBy))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .Join("entity e", on: "t.entity = e.id")
            .LeftJoin("employee em", "t.custbody_dbti_prepared_by = em.id")
            .WithFilters(
                Equal("t.tranid", id),
                Equal("tl.mainline", "T"))
            .WithFilters(PackingVendorReturnAuthorizationFilters())
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<VendorReturnAuthorizationPackingHeaderNSDTO>(query.Query, query.Limit, query.Offset);
        var nsdto = response.items.FirstOrDefault();

        return nsdto is null ? null : MapInfoDto(nsdto);
    }

    public async Task<(IEnumerable<VendorReturnAuthorizationLineDTO> Data, int Count)> GetPackingVendorReturnAuthorizationLines(string id, DataGridIntent intent)
    {
        var mobileLineQuery = sqlQuery.ResolveSuiteQLScript(
            "NS_VendorReturnAuthorization_Get_Items",
            new Dictionary<string, string>
            {
                ["tranid"] = id
            });

        var query = builderFactory.Create()
            .Select(
                ("q.MaterialCode", nameof(VendorReturnAuthorizationPackingLineNSDTO.ItemCode)),
                ("q.MaterialName", nameof(VendorReturnAuthorizationPackingLineNSDTO.ItemDescription)),
                ("q.UoMName", nameof(VendorReturnAuthorizationPackingLineNSDTO.UoM)),
                ("q.LocationName", nameof(VendorReturnAuthorizationPackingLineNSDTO.Warehouse)),
                ("q.LineQuantity", nameof(VendorReturnAuthorizationPackingLineNSDTO.QuantityPlanned)),
                ("q.LineQuantityPacked", nameof(VendorReturnAuthorizationPackingLineNSDTO.QuantityReceived))
            )
            .From($"({mobileLineQuery}) q")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<VendorReturnAuthorizationPackingLineNSDTO>(netsuiteService);

        return (response.items.Select(MapLineDto), response.totalResults);
    }

    private static AppFilterDescriptor[] PackingVendorReturnAuthorizationFilters()
    {
        return
        [
            Equal("t.recordtype", "vendorreturnauthorization"),
            In("t.status", new string[] { "B", "E" })
        ];
    }

    private static VendorReturnAuthorizationDataGridDTO MapDataGridDto(VendorReturnAuthorizationPackingDataGridNSDTO nsdto)
    {
        return new()
        {
            Id = nsdto.Id,
            ReferenceNumber = nsdto.ReferenceNumber,
            Date = nsdto.Date,
            SourceSubsidiary = nsdto.SourceSubsidiary,
            DestinationSubsidiary = nsdto.DestinationSubsidiary,
            Location = nsdto.Location,
            TransferLocation = nsdto.TransferLocation,
            Status = nsdto.Status,
            Remarks = nsdto.Remarks
        };
    }

    private static VendorReturnAuthorizationInfoDTO MapInfoDto(VendorReturnAuthorizationPackingHeaderNSDTO nsdto)
    {
        return new()
        {
            Id = nsdto.Id,
            Date = nsdto.Date,
            ReferenceNumber = nsdto.ReferenceNumber,
            FromSubsidiary = nsdto.FromSubsidiary,
            Location = nsdto.Location,
            TransferLocation = nsdto.TransferLocation,
            PreparedBy = nsdto.PreparedBy,
        };
    }

    private static VendorReturnAuthorizationLineDTO MapLineDto(VendorReturnAuthorizationPackingLineNSDTO nsdto)
    {
        return new()
        {
            ItemCode = nsdto.ItemCode,
            ItemDescription = nsdto.ItemDescription,
            UoM = nsdto.UoM,
            Warehouse = nsdto.Warehouse,
            QuantityPlanned = nsdto.QuantityPlanned,
            QuantityReceived = nsdto.QuantityReceived,
        };
    }
}
