SELECT
	t.id AS NetsuiteOrderInternalId,
	t.tranid AS OrderNumber,
	t.recordtype AS OrderType,
	t.status AS OrderStatus,
	tl.createdfrom AS BaseDoc,
	t.custbody_dbti_linked_str_num AS LinkedSTR,

	t.subsidiary AS NetsuiteSubsidiaryInternalId,

	tl.location AS NetsuiteLocationInternalId,
	loc.name AS LocationName,
	loc.usebins as LocationUsedBin,

	tl.id AS LineSequenceNumber,
	tl.transactionlinetype as TransactionLineType,

	t.entity AS NetsuiteVendorInternalId,
	e.fullname AS VendorName,
	ba.custrecord_dbti_vba_assigned_bin AS NetsuiteMaterialVendorAssignedBin,
	ibv1.quantityavailable AS VendorAssignedBinQuantityAvailableGood,
	ibv2.quantityavailable AS VendorAssignedBinQuantityAvailableBad,

	i.id AS NetsuiteMaterialInternalId,
	i.itemid as MaterialCode,
	i.displayname as MaterialName,

	b.id AS NetsuiteMaterialPrefferedBinId,
	ibi1.quantityavailable AS PreferredBinQuantityAvailableGood,
	ibi2.quantityavailable AS PreferredBinQuantityAvailableBad,

	ibli1.quantityavailable AS LocationItemQuantityAvailableGood,
    ibli2.quantityavailable AS LocationItemQuantityAvailableBad,

	i.weight AS MaterialWeight,
	ail.quantityavailable as LocationItemQuantityAvailable,

	ABS(tl.quantity) AS LineQuantity,
	tl.quantityshiprecv AS LineQuantityPacked,
	tl.quantitybackordered AS LineQuantityBackOrdered,

	tl.units AS NetsuiteUoMInternalId,
	uom.unitname AS UoMName,
	uom.conversionrate AS UoMRate,

	TO_CHAR(t.custbody_dbti_est_receipt_date, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderDocumentDate,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
FROM item i
JOIN transactionline tl ON i.id = tl.item
JOIN transaction t ON tl.transaction = t.id
JOIN entity e ON t.entity = e.id
JOIN subsidiary s ON t.subsidiary = s.id
JOIN location loc ON tl.location = loc.id
JOIN unitstypeuom uom ON tl.units = uom.internalid

LEFT JOIN customrecord_dbti_vendor_bin_assignment ba ON t.entity = ba.custrecord_dbti_vba_vendor

LEFT JOIN (
	SELECT
		ibq.item,
		ibq.bin,
		b.location
	FROM itembinquantity ibq
	JOIN bin b ON b.id = ibq.bin
	WHERE ibq.preferredbin = 'T'
) ibq ON ibq.item = i.id  AND ibq.location = tl.location

LEFT JOIN bin b ON b.id = ibq.bin

LEFT JOIN (
	SELECT
		item,
		BUILTIN.DF( item ) AS itemname,
		location,
		BUILTIN.DF( location ) AS locationname,
		quantityavailable
	FROM
		AggregateItemLocation
	ORDER BY
		Item,
		Location
) ail ON ail.item = i.id  AND ail.location = tl.location 

LEFT JOIN inventorybalance ibv1
    ON ibv1.item = i.id
    AND ibv1.location = tl.location
    AND ibv1.binnumber =
        ba.custrecord_dbti_vba_assigned_bin
    AND ibv1.inventorystatus = '1'

LEFT JOIN inventorybalance ibv2
    ON ibv2.item = i.id
    AND ibv2.location = tl.location
    AND ibv2.binnumber =
        ba.custrecord_dbti_vba_assigned_bin
    AND ibv2.inventorystatus = '3'

LEFT JOIN inventorybalance ibi1
    ON ibi1.item = i.id
    AND ibi1.location = tl.location
    AND ibi1.binnumber = b.id
    AND ibi1.inventorystatus = '1'

LEFT JOIN inventorybalance ibi2
    ON ibi2.item = i.id
    AND ibi2.location = tl.location
    AND ibi2.binnumber = b.id
    AND ibi2.inventorystatus = '3'

LEFT JOIN (
    SELECT
        ib.item,
        ib.location,
        ib.inventorystatus,
        SUM(ib.quantityavailable) AS quantityavailable
    FROM
        inventorybalance ib
    GROUP BY
        ib.item,
        ib.location,
        ib.inventorystatus
) ibli1 ON ibli1.item = i.id 
    AND ibli1.location = tl.location
    AND ibli1.inventorystatus = '1'

LEFT JOIN (
    SELECT
        ib.item,
        ib.location,
        ib.inventorystatus,
        SUM(ib.quantityavailable) AS quantityavailable
    FROM
        inventorybalance ib
    GROUP BY
        ib.item,
        ib.location,
        ib.inventorystatus
) ibli2 ON ibli2.item = i.id 
    AND ibli2.location = tl.location
    AND ibli2.inventorystatus = '3'
WHERE
	t.recordtype = 'vendorreturnauthorization'
	AND t.status IN ('B', 'E')
	AND t.tranid = @tranid