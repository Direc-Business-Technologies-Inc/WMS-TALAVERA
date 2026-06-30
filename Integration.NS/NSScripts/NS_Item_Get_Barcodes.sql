SELECT 
	i.id as NetsuiteMaterialInternalId,
	i.itemid as MaterialCode,
	i.displayname as MaterialName,
	bc.name as MaterialBarcode,
	
	uom.unitname AS UoMName,
	uom.conversionrate AS UoMRate
FROM
	item i
	LEFT JOIN customrecord_barcode_per_uom bc ON i.id = bc.custrecord_bpu_item
	LEFT JOIN unitstypeuom uom ON bc.custrecord_bpu_uom = uom.internalid
WHERE
	i.id IN ({items})