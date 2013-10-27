Feature: Implied Price

Scenario: First Generation - Imply In to Bid
  When a market place has outright orders:
   | Contract | Bid Qty | Bid Price | Offer Price | Offer Qty |
   | V1       | 1       | 9505      |             |           |
   | V2       |         |           | 9503        | 1         |
  Then the market place has synthetic orders:
   | Contract | Bid Qty | Bid Price | Offer Price | Offer Qty |
   | V1 - V2  | 1       | 02        |             |           |

Scenario: First Generation - Imply In to Offer
  When a market place has outright orders:
   | Contract | Bid Qty | Bid Price | Offer Price | Offer Qty |
   | V1       |         |           | 9505        | 1         |
   | V2       | 1       | 9503      |             |           |
  Then the market place has synthetic orders:
   | Contract | Bid Qty | Bid Price | Offer Price | Offer Qty |
   | V1 - V2  |         |           | 02          | 1         |
