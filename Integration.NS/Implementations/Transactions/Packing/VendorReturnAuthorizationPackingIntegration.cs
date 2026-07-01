using Application.DataTransferObjects.Transactions.Packing.VendorReturnAuthorization;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using Integration.NS.DataTransferObjects.Packing.VendorReturnAuthorization;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Shared.Entities;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

namespace Integration.NS.Implementations.Transactions.Packing;

internal class VendorReturnAuthorizationPackingIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : IVendorReturnAuthorizationPackingIntegration
{
    public async Task<(IEnumerable<VendorReturnAuthorizationDataGridDTO> Data, int Count)> GetPackingVendorReturnAuthorizationsList(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.Id)),
                ("t.tranid", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.ReferenceNumber)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(VendorReturnAuthorizationPackingDataGridNSDTO.Date)),
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
            .WithFilters(Equal("tl.mainline", "T"))
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
                ("BUILTIN.DF(t.tosubsidiary)", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.ToSubsidiary)),
                ("BUILTIN.DF(tl.location)", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.Location)),
                ("BUILTIN.DF(t.transferlocation)", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.TransferLocation)),
                ("t.custbody_dbti_prepared_by", nameof(VendorReturnAuthorizationPackingHeaderNSDTO.PreparedBy))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .Join("entity e", on: "t.entity = e.id")
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
        var query = builderFactory.Create()
            .Select(
                ("item.itemid", nameof(VendorReturnAuthorizationPackingLineNSDTO.ItemCode)),
                ("item.displayname", nameof(VendorReturnAuthorizationPackingLineNSDTO.ItemDescription)),
                ("BUILTIN.DF(tl.units)", nameof(VendorReturnAuthorizationPackingLineNSDTO.UoM)),
                ("BUILTIN.DF(tl.location)", nameof(VendorReturnAuthorizationPackingLineNSDTO.Warehouse)),
                ("tl.quantity", nameof(VendorReturnAuthorizationPackingLineNSDTO.QuantityPlanned))
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .Join("entity e", on: "t.entity = e.id")
            .WithFilters(
                Equal("t.tranid", id),
                Equal("tl.mainline", "F"))
            .WithFilters(PackingVendorReturnAuthorizationFilters())
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
            In("t.status", new string[] { "B" })
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
            ToSubsidiary = nsdto.ToSubsidiary,
            Location = nsdto.Location,
            TransferLocation = nsdto.TransferLocation,
            PreparedBy = nsdto.PreparedBy,
            ReceivedBy = nsdto.ReceivedBy
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
            QuantityPlanned = nsdto.QuantityPlanned
        };
    }
}
