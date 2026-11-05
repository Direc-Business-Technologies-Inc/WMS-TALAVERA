SELECT DISTINCT tl.custcol_dbti_vendor AS Id
FROM transactionline tl
JOIN transaction t ON tl.transaction = t.id
WHERE t.recordtype IN ('intercompanytransferorder', 'transferorder')
  AND t.custbody_dbti_transfer_category IN ('1', '2')
  AND t.status IN ('B', 'D', 'E')
  AND tl.transactionlinetype = 'SHIPPING'
  AND tl.custcol_dbti_vendor IS NOT NULL
  AND t.tranid = @tranid
