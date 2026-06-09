SELECT
	t.id AS NetsuiteOrderInternalId,
    t.tranid AS OrderNumber,
    t.recordtype AS OrderType,
    t.status AS OrderStatus,

    tl.location AS NetsuiteFromLocationInternalId,
    t.transferlocation AS NetsuiteToLocationInternalId,
	
    t.subsidiary AS NetsuiteFromSubsidiaryInternalId,
	s.name AS FromSubsidiaryName,

	s.custrecord_dbti_default_bo_location AS NetsuiteSubsidiaryDefaultBOInternalId,
    t.tosubsidiary AS NetsuiteToSubsidiaryInternalId,

    loc.name AS LocationName,
    loc.usebins AS IsLocationUsedBin,

    tl.id AS LineSequenceNumber,
    tl.transactionlinetype AS TransactionLineType,

    tl.item AS NetsuiteMaterialInternalId,
    i.itemid AS MaterialCode,
    i.displayname AS MaterialName,
    b.id AS NetsuiteMaterialPrefferedBinId,
	i.weight AS MaterialWeight,

    tl.quantity AS LineQuantity,
	tl.quantityshiprecv AS LineQuantityReceived,

    tl.units AS NetsuiteUoMInternalId,
    uom.unitname AS UoMName,
    uom.conversionrate AS UoMRate,

    TO_CHAR(
        t.createddate,
        'YYYY-MM-DD"T"HH24:MI:SS'
    ) AS NetsuiteOrderCreatedDate

FROM item i
    JOIN transactionline tl ON i.id = tl.item
    JOIN transaction t ON tl.transaction = t.id
    JOIN subsidiary s ON t.subsidiary = s.id
    JOIN location loc ON tl.location = loc.id
    LEFT JOIN (
	   SELECT
		   ibq.item,
		   ibq.bin,
		   b.location
	   FROM itembinquantity ibq
	   JOIN bin b
		   ON b.id = ibq.bin
	   WHERE ibq.preferredbin = 'T'
   ) ibq ON ibq.item = i.id  AND ibq.location = tl.location

	LEFT JOIN bin b ON b.id = ibq.bin
    JOIN unitstypeuom uom ON tl.units = uom.internalid

WHERE
    t.recordtype IN ('intercompanytransferorder', 'transferorder')
    AND t.custbody_dbti_transfer_category IN ('1', '2')
    AND t.status IN ('F', 'E')
	AND tl.transactionlinetype = 'RECEIVING'
	AND t.tranid = @tranid