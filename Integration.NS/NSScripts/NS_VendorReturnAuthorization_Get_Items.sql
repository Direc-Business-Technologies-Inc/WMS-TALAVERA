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

	i.id AS NetsuiteMaterialInternalId,
	i.itemid as MaterialCode,
	i.displayname as MaterialName,
	b.id AS NetsuiteMaterialPrefferedBinId,
	i.weight AS MaterialWeight,
	ib1.quantityavailable as LocationItemQuantityAvailable,

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
	) ib1 ON ib1.item = i.id  AND ib1.location = tl.location 
WHERE
	t.recordtype = 'vendorreturnauthorization'
	AND t.status IN ('B', 'E')
	AND t.tranid = @tranid