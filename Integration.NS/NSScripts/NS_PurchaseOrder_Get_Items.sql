SELECT
	t.id AS NetsuiteOrderInternalId,
	t.tranid AS OrderNumber,
	t.recordtype AS OrderType,
	t.status AS OrderStatus,
	
	t.subsidiary AS NetsuiteSubsidiaryInternalId,
	s.custrecord_dbti_default_bo_location AS NetsuiteSubsidiaryDefaultBOInternalId,
	
	tl.location AS NetsuiteLocationInternalId,
	loc.name AS LocationName,
	loc.usebins as LocationUsedBin,
	
	tl.id AS LineSequenceNumber,
	tl.transactionlinetype as TransactionLineType,
	
	t.entity AS NetsuiteVendorInternalId,
	e.fullname AS VendorName,
	ivb.custrecord_dbti_vba_assigned_bin AS VendorBinAssignmentId,
	
	i.id AS NetsuiteMaterialInternalId,
	tl.item as MaterialInternalId,
	i.itemid as MaterialCode,
	i.displayname as MaterialName,
	b.id AS NetsuiteMaterialPrefferedBinId,
	i.weight AS MaterialWeight,
	tl.quantity AS LineQuantity,
	tl.quantityshiprecv AS LineQuantityReceived,
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
    LEFT JOIN (
	   SELECT
		   ibq.item,
		   MIN(ibq.bin) AS bin,
		   b.location
	   FROM itembinquantity ibq
	   JOIN bin b
		   ON b.id = ibq.bin
	   WHERE ibq.preferredbin = 'T'
	   GROUP BY ibq.item, b.location
   ) ibq ON ibq.item = i.id  AND ibq.location = tl.location
	LEFT JOIN bin b ON b.id = ibq.bin
	LEFT JOIN (
		SELECT
			iv.item,
			iv.subsidiary,
			MIN(ba.custrecord_dbti_vba_assigned_bin) AS custrecord_dbti_vba_assigned_bin,
			ba.custrecord_dbti_vba_location
		FROM itemvendor iv
		JOIN customrecord_dbti_vendor_bin_assignment ba ON iv.vendor = ba.custrecord_dbti_vba_vendor
		WHERE iv.preferredvendor = 'T'
		GROUP BY iv.item, iv.subsidiary, ba.custrecord_dbti_vba_location
	) ivb ON ivb.item = i.id
		AND ivb.subsidiary = t.subsidiary
		AND ivb.custrecord_dbti_vba_location = tl.location
    JOIN unitstypeuom uom ON tl.units = uom.internalid
WHERE
	t.recordtype = 'purchaseorder'
	AND t.status IN ('B', 'E')			
	AND t.tranid = @tranid