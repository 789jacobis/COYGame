# COY 文件與程式落差整理

更新日期：2026-06-29

本文件用來對照 `COY卡牌機制規劃_v2.docx`、`COYGame玩法介紹_v2.docx` 與目前 Unity 程式實作狀態。目標不是重寫企劃，而是標記「已完成」、「部分完成」、「尚未完成」與「建議下一步」。

## 目前整體狀態

目前程式已經完成 MVP 戰鬥主循環，並建立出可擴充的 V2 卡牌規則骨架。

已完成的主要系統：

- 6 回合戰鬥流程。
- 玩家進攻、玩家防禦、敵方進攻、敵方防禦。
- 進攻牌庫與防禦牌庫分開。
- Deck / Hand / Discard / Reserved / OutsideGame 區域。
- AP、抽牌、洗牌、護盾、籃框 HP、得分判定。
- 擊破籃框時攻擊階段自動結束。
- 卡牌 V2 effect list。
- Trigger / Condition / Target / Action / Duration 基礎架構。
- Team / Player / Card 層級的 Status / Modifier。
- Editor validation 與 V2 smoke test 工具。
- 拖曳式 PlayerChoice 手牌選牌流程。
- 真實卡牌已開始導入 PlayerChoice / NextCard。

目前剩餘落差主要不是「大骨架缺失」，而是：

- 少數 target/action 的語意還可以更精細。
- PlayerChoice 選球員與敵方隱藏手牌仍需要額外 UI 設計。
- 真實卡牌資料尚未大量重整，因此需要後續卡牌設計與數值驗證。

## 已對齊

### Round / Phase

文件定義：

- Round：完整輪次，玩家與敵方完成對應攻防流程後增加。
- Phase：單次可操作階段，例如己方攻擊階段、己方防禦階段。

程式現況：

- `BattlePhase` 已區分敵方防禦、玩家選策略、玩家攻擊、玩家防禦、敵方選策略、敵方攻擊與結果階段。
- `Round` 由 `BattleController` 管理。
- 卡牌效果與 status duration 主要以 phase 為單位運作。

狀態：已對齊。

### 卡牌資料結構

文件定義：

- CardData：名稱、類型、AP、描述、關鍵字、效果列表。
- 每張卡可由多個 Effects 組合。
- 戰鬥中使用 CardRuntime 表示卡牌實例。

程式現況：

- `CardData` 已有 `cardName`、`rulesText`、`cardType`、`artwork`、`tags`、`keywordRules`、`apCost`、`usablePhase`、`effects`。
- `CardRuntime` 已持有 `CurrentCost`、`RemainingRecycleCount`、`WasPlayed`、`Statuses`。
- `CardEffectData` 已有 `trigger`、`conditions`、`target`、`action`、`duration`。
- 舊式欄位仍保留作為相容 fallback，但目前卡牌資料已轉為 V2 effect。

狀態：已對齊，但 legacy 欄位尚未移除。

### 卡牌類型

文件定義：

- Attack
- Defense
- Universal
- Item

程式現況：

- enum 已完整存在。
- Battle runtime 已依進攻牌庫、防禦牌庫與 Item usablePhase 判斷卡牌是否可在該階段出現。
- Rebound 已作為 Item 類卡牌使用。

狀態：已對齊。

### 基礎戰鬥與牌庫系統

文件定義：

- 進攻、防禦牌庫分開。
- 階段開始抽牌。
- 使用牌進 Discard。
- Exhaust / Once 進 OutsideGame。
- Retain 可保留到下一個可出現階段。
- Discard 不足時洗回 Deck。

程式現況：

- `TeamRuntime` 建立進攻牌庫與防禦牌庫。
- `DeckRuntime` 管理 `Deck`、`DiscardPile`、`Reserved`、`OutsideGame`。
- `DeckRuntime.Draw` 支援 deck 空時洗回 discard。
- `DeckRuntime.DiscardCard` 支援 Exhaust / Once / Retain 的主要區域流向。

狀態：已對齊。

### Trigger

目前已支援：

- `OnPlay`
- `OnDraw`
- `OnDiscard`
- `OnCreate`
- `OnRemove`
- `OnPhaseEnd`
- `OnNextOwnPhaseStart`
- `OnEveryOwnPhaseStart`
- `OnOwnAttackPhaseStart`
- `OnOwnAttackPhaseEnd`
- `OnOwnDefensePhaseStart`
- `OnOwnDefensePhaseEnd`
- `OnAnyOwnPhaseStart`
- `OnAnyOwnPhaseEnd`
- `OnHoopBroken`
- `OnHoopDamaged`

狀態：已對齊。

### Condition

目前已支援：

- `PlayerChose2PT`
- `PlayerChose3PT`
- `EnemyChose2PT`
- `EnemyChose3PT`
- `ActingTeamChose2PT`
- `ActingTeamChose3PT`
- `HoopBrokenByThisCard`
- `CardWasNotPlayedThisPhase`
- `HasCardInHand`
- `HasEnoughAP`
- `CardCostGreaterThan`
- `TargetHasStatus`
- `StatusStackAtLeast`

程式現況：

- `HoopBrokenByThisCard` 已透過 `EffectEventContext` 判斷是否由本張卡觸發。
- Status 條件已可檢查 Team / Player / Card 目標。

狀態：已對齊。

### TargetSide / TargetKind

文件定義：

- Side：Self / Opponent / Both。
- Kind：Team / Player / Card / Hoop / Zone。

程式現況：

- Team target 支援 Self / Opponent / Both。
- Player target 支援 Self / Opponent / Both。
- Card target 支援 Self / Opponent / Both，並可指定 Hand / DrawPile / DiscardPile / Reserved / OutsideGame。
- Hoop target 目前仍使用當前 active target hoop。
- Zone enum 存在，但目前主要透過 Card target 的 `zone` 欄位操作。

狀態：大致已對齊。

備註：

- `Both + Hoop` 仍會被 validation 警告，因為目前籃框效果只解析當前攻防流程中的 active target hoop，不會同時操作雙方籃框。

### TargetSelector

目前已支援：

- `ThisCard`
- `NextCard`
- `Random`
- `All`
- `PlayerChoice`
- `HighestAttackPlayer`
- `LowestCostCard`
- `CardsInZone`
- `OwnerPlayer`
- `ActingTeam`
- `OpponentTeam`

程式現況：

- `PlayerChoice + Card target` 已支援拖曳式手牌選牌：進入選牌模式後，場面變暗、手牌保持可拖曳，玩家把目標牌拖到畫面中段並用 YES / NO 確認。
- `PlayerChoice + Player target` 仍保留簡易選項 UI。
- AI 或無 UI 選擇時，`PlayerChoice` 會 fallback 到第一個合法目標，避免流程卡住。
- `NextCard` 已支援「來源卡打出後，原本手牌順序中的下一張卡」。
- 玩家出牌已支援逐段結算 effect，遇到 `PlayerChoice` 時才暫停選擇，因此可正確處理「先抽牌，再選牌」這類效果。

狀態：已支援第一版。

備註：

- `PlayerChoice` 選手牌已不是按鈕 UI；選球員目前仍是按鈕 UI。
- `NextCard` 目前不是「下一次實際打出的牌」監聽器，而是「目前手牌順序中的下一張候選卡」。

### Duration

目前已支援：

- `Instant`
- `CurrentPhase`
- `OwnAttackPhases`
- `OwnDefensePhases`
- `AnyOwnPhases`
- `FullRounds`
- `ThisGame`
- `UntilUsed`
- `UntilTriggered`

程式現況：

- Team / Player / Card status 都可使用 duration。
- phase end tick 會處理階段型 duration。
- `FullRounds` 已用兩個 own phase end 作為一個完整攻防輪次的倒數基準。
- `ThisGame` 不會因 phase tick 消失。
- `UntilUsed` / `UntilTriggered` 會在對應 modifier 被使用後消耗。

狀態：已對齊第一版。

### Status / Modifier

文件定義：

- Status 可掛在 Team / Player / Card。
- Modifier 用於改變攻擊、受擊、護盾、AP、抽牌、卡牌費用等行為。

程式現況：

- 已新增 `StatusRuntime`、`StatusModifierRuntime`、`StatusContainer`。
- `TeamRuntime`、`PlayerRuntime`、`CardRuntime` 已可持有 status。
- 已支援 `ApplyTeamStatus`、`ApplyPlayerStatus`、`ApplyCardStatus`、`ApplyModifier`、`ClearStatus`。
- 已支援 status stack、duration、phase end tick、使用後消耗。
- 已支援 modifier 類型包含攻擊增傷、受擊減傷、護盾增幅、AP、Max AP、抽牌數、卡牌費用等。
- 舊式 TurnContext modifier 已大致搬到 Status / Modifier 流程。

狀態：已對齊第一版。

### Action

目前已支援：

- `DealDamage`
- `RepeatDamage`
- `ModifyDamage`
- `GainShield`
- `ModifyShield`
- `ModifyAvailableAP`
- `ModifyMaxPhaseAP`
- `ModifyNextOwnPhaseAP`
- `ModifyCardCost`
- `DrawCards`
- `ModifyDrawCount`
- `DiscardCards`
- `GenerateCard`
- `CopyCard`
- `MoveCard`
- `RemoveCard`
- `PlayCard`
- `PlayRandomCards`
- `ApplyTeamStatus`
- `ApplyPlayerStatus`
- `ApplyCardStatus`
- `ApplyModifier`
- `ClearStatus`

狀態：已對齊第一版。

備註：

- `CopyCard` 目前複製 CardData 建立新的 CardRuntime，不保留來源卡戰鬥中的暫時狀態。
- `PlayCard` / `PlayRandomCards` 會依 action 設定決定是否消耗 AP、是否觸發 OnPlay、是否在籃框擊破後停止。

## 部分對齊

### PlayerChoice UI

程式現況：

- 已可讓玩家透過拖曳手牌選擇 Card 目標。
- 已可讓玩家透過選項按鈕選擇 Player 目標。
- 選完後會把結果放入 `TurnContext`，resolver 依選擇結算。
- 已導入真實卡牌：`Outlet Pass` 使用 `PlayerChoice + ModifyCardCost`。

剩餘落差：

- Card target 尚未做候選目標高亮。
- Player target 尚未支援直接點擊球員 sprite。
- 尚未支援玩家直接選敵方隱藏手牌。

建議：

- 若後續大量使用 PlayerChoice，優先補候選目標高亮。
- 若出現選球員技能，再把 Player target UI 升級成點球員 sprite。

### NextCard 語意

程式現況：

- `NextCard` 目前代表「來源卡打出後，原本手牌順序中的下一張候選卡」。
- 已導入真實卡牌：`Pick & Roll` 使用 `NextCard + ModifyCardCost`。

剩餘落差：

- 文件若想表達「下一次實際打出的牌」，目前還不是這個語意。
- 「下一次實際打出的牌」比較像一種 status / modifier 監聽器，而不是單純 target selector。

建議：

- 保留目前 `NextCard` 作為手牌順序目標。
- 若需要「下一次出牌」效果，另外用 `UntilUsed` status 或新的 trigger/modifier 表示。

### Zone Target

程式現況：

- Card target 可透過 `zone` 欄位選 Hand / DrawPile / DiscardPile / Reserved / OutsideGame。
- `MoveCard` 可在不同區域間移動卡牌。

剩餘落差：

- `TargetKind.Zone` 目前尚未成為獨立 resolver。
- 現階段多數區域操作其實是「選某區域內的卡」，而不是「選區域本身」。

建議：

- 若未來需要「對整個區域施加規則」，再實作真正的 Zone target。
- 目前新增卡牌時，優先使用 Card target + zone。

### Opponent Card Zone Target

程式現況：

- `TurnContext` 已有 `OpposingDeck`、`OpposingHand`。
- `TargetResolver` 已可依 `TargetSide.Opponent` 取得敵方區域卡牌。

剩餘落差：

- 玩家 UI 目前不顯示敵方卡牌正面，因此若玩家要選敵方手牌，會需要額外 UI 設計。
- AI 階段可正常用規則選取，不需要讓玩家看到內容。

建議：

- 先避免設計「玩家手動指定敵方隱藏手牌」的卡。
- 可以設計「隨機丟棄敵方一張手牌」、「敵方棄牌堆移除一張牌」這類不需要玩家看內容的效果。

## 程式已有但文件可補充的行為

### Validation 強化

目前程式有：

- `PlayerChoice` 只能搭配 Card / Player target。
- `PlayerChoice + Opponent Hand` 會警告，因為敵方手牌目前是隱藏資訊。
- `PlayerChoice + Card target` 若不是 Hand 區域會警告。
- `NextCard` 必須搭配 Card target。
- `NextCard` 若不是 Hand 區域會警告。
- `ModifyCardCost`、`DiscardCards`、`MoveCard`、`CopyCard`、`PlayCard`、`PlayRandomCards` 會檢查是否搭配 Card target。
- `ApplyTeamStatus`、`ApplyPlayerStatus`、`ApplyCardStatus` 會檢查對應 target kind。
- `ApplyModifier` / `ClearStatus` 會限制在 Team / Player / Card 目標。

文件可補：

- 新增卡牌時，target selector 與 action/target 搭配錯誤應視為資料錯誤，而不是 runtime 才處理。

### Rebound 機制

目前程式有：

- 若防守成功，下一次自己進攻時額外獲得 Rebound 卡。
- Rebound 是 Item 卡。
- Rebound 消耗 0 AP，增加 1 AP，並有 Exhaust / 消滅性質。

文件可補：

- 防守成功獎勵規則。
- Rebound 是否能被抽牌、複製、保留、回收等效果影響。

### Card Artwork

目前程式有：

- `CardData.artwork`。
- 依球員或道具套用暫時卡面圖。
- CardView 顯示整張卡圖作為背景。

文件可補：

- 卡牌視覺資料不是戰鬥規則，但屬於 CardData 的展示欄位。

### Editor Tools

目前程式有：

- `Tools > COY > Validate Card Data`
- `Tools > COY > Run V2 Card Smoke Tests`
- `Tools > COY > Build MVP Scene`
- `Tools > COY > Repair Current Scene`
- `Tools > COY > Apply Sprite Visuals To Battle Scene`
- `Tools > COY > Generate Placeholder Card Artwork`

文件可補：

- 新增或修改卡牌資料後必跑 validation。
- 修改 V2 resolver / target / status / duration 後必跑 smoke tests。

## 尚未完整支援或暫不急

### 直接點選式目標 UI

目前 `PlayerChoice` 是按鈕 UI，不是直接點選戰場物件。

優先度：中。

建議等真實卡牌需要大量手動選擇時再做。

### 更完整的 AI 策略

目前 AI 可依規則自動出牌，但不是深度策略 AI。

優先度：中低。

建議等卡牌資料穩定後再做。

### Zone 作為獨立目標

目前區域操作以 Card target + zone 解決。

優先度：低。

除非出現「整個棄牌堆獲得狀態」、「牌庫本身被封鎖」這種設計，否則不急。

### 完整動畫與美術

目前已有 sprite 佔位與卡牌背景圖，但不是正式美術。

優先度：低。

規則語言與卡牌資料穩定後再投入比較好。

## 建議下一步順序

### 1. 大量整理卡牌資料

原因：

- 規則語言、PlayerChoice / NextCard、validation 防呆都已有第一版。
- 現在適合開始檢查 64 張卡是否符合球員定位與隊伍流派。

建議內容：

- 每名球員重新檢查 2 張普通攻擊、4 張專屬攻擊、2 張普通防禦、4 張專屬防禦。
- 將技能盡量改成通用 action / status / target 組合。
- 跑 validation 與 smoke tests。

### 2. 實測 UI 操作感

原因：

- `PlayerChoice + Card target` 已完成拖曳式選牌，但還需要更多真實卡牌驗證。

建議內容：

- 看候選目標是否需要高亮。
- 看 YES / NO 確認是否太多一步。
- 看選牌時暗幕、提示文字、手牌拖曳是否足夠清楚。

### 3. 更新文件與卡牌規則用語

原因：

- 程式已經比 v2 文件更具體，文件應避免留下模糊語意。

建議內容：

- 明確區分 `NextCard` 與「下一次實際打出的牌」。
- 明確記錄 PlayerChoice Card / Player 的不同 UI。
- 明確記錄 validation 規則。

### 4. 補 UI 精修

原因：

- 目前選牌流程可用，但還不是最終表現。

建議內容：

- Player target 改成點球員 sprite。
- 候選卡牌高亮。
- 更好的提示與確認視覺。

## 目前不急著做

- 完整動畫系統。
- 完整卡牌美術。
- 聯機。
- 商店、養成、裝備。
- 複雜 AI 策略樹。

目前最重要的是先讓卡牌規則語言穩定，然後用真實卡牌資料反覆驗證。
