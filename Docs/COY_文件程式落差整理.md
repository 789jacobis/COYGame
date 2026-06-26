# COY 文件與程式落差整理

更新日期：2026-06-26

本文件用來對照 `COY卡牌機制規劃_v2.docx`、`COYGame玩法介紹_v2.docx` 與目前 Unity 程式實作狀態。目標不是重寫企劃，而是標記「已完成」、「部分完成」、「尚未完成」與「建議下一步」。

## 目前整體狀態

目前程式已經支援 MVP 戰鬥主循環與 V2 卡牌資料骨架：

- 6 回合戰鬥流程。
- 玩家進攻、玩家防禦、敵方進攻、敵方防禦。
- 攻擊牌庫與防禦牌庫分開。
- Deck / Hand / Discard / Reserved / OutsideGame 區域。
- AP、抽牌、洗牌、護盾、籃框 HP、得分判定。
- 擊破籃框時攻擊階段自動結束。
- 卡牌 V2 effect list。
- 基本 trigger / condition / target / action / duration 欄位。
- Editor validation 與 smoke test 工具。

目前最大落差在於：文件已經描述出完整「可長期擴充的卡牌規則語言」，程式目前則是先支援現有卡牌會用到的核心子集。

## 已對齊

### Round / Phase

文件定義：

- Round：完整輪次，玩家與敵方完成對應攻防流程後增加。
- Phase：單次可操作階段，例如己方攻擊階段、己方防禦階段。

程式現況：

- `BattlePhase` 已區分敵方防禦、玩家選策略、玩家攻擊、玩家防禦、敵方選策略、敵方攻擊與結果階段。
- `Round` 由 `BattleController` 管理。
- 多數效果目前以 phase 為單位處理。

狀態：已對齊。

### 卡牌資料結構

文件定義：

- CardData：名稱、類型、AP、描述、關鍵字、效果列表。
- 每張卡可由多個 Effects 組合。

程式現況：

- `CardData` 已有 `cardName`、`rulesText`、`cardType`、`artwork`、`tags`、`keywordRules`、`apCost`、`usablePhase`、`effects`。
- `CardEffectData` 已有 `trigger`、`conditions`、`target`、`action`、`duration`。
- 舊式欄位仍保留作為相容 fallback，但目前卡牌資料已轉為 V2 effect。

狀態：已對齊，但 legacy 欄位未移除。

### 卡牌類型

文件定義：

- Attack
- Defense
- Universal
- Item

程式現況：

- enum 已完整存在。
- Battle runtime 已依攻擊牌庫、防禦牌庫與 Item usablePhase 判斷卡牌是否可在該階段出現。
- Rebound 已作為 Item 類卡牌使用。

狀態：已對齊。

### 基礎戰鬥與牌庫系統

文件定義：

- 攻擊、防禦牌庫分開。
- 階段開始抽牌。
- 使用牌進 Discard。
- Exhaust 進 OutsideGame。
- Discard 不足時洗回 Deck。

程式現況：

- `TeamRuntime` 建立攻擊牌庫與防禦牌庫。
- `DeckRuntime` 管理 `Deck`、`DiscardPile`、`Reserved`、`OutsideGame`。
- `DeckRuntime.Draw` 支援 deck 空時洗回 discard。
- `DeckRuntime.DiscardCard` 支援 Exhaust / Once / Retain 的主要區域流向。

狀態：已對齊。

### 基礎 Action

文件中的下列通用行為目前已支援：

- `DealDamage`
- `GainShield`
- `ModifyAvailableAP`
- `ModifyMaxPhaseAP`
- `ModifyNextOwnPhaseAP`
- `ModifyCardCost`
- `DrawCards`
- `ModifyDrawCount`
- `DiscardCards`
- `GenerateCard`
- `MoveCard`
- `RemoveCard`
- `ModifyDamage`

狀態：現有卡牌需要的核心動作已對齊。

### 目前 Trigger

程式目前已接上：

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

狀態：enum 與流程觸發點大致已對齊。

## 部分對齊

### Condition 條件

文件定義：

- Strategy 相關條件。
- HoopBrokenByThisCard。
- CardWasNotPlayedThisPhase。
- HasCardInHand。
- HasEnoughAP。
- TargetHasStatus。
- StatusStackAtLeast。

程式現況：

- 已支援：`PlayerChose2PT`、`PlayerChose3PT`、`EnemyChose2PT`、`EnemyChose3PT`、`ActingTeamChose2PT`、`ActingTeamChose3PT`、`CardWasNotPlayedThisPhase`、`HasCardInHand`、`HasEnoughAP`、`CardCostGreaterThan`、`TargetHasStatus`、`StatusStackAtLeast`。
- 部分支援：`HoopBrokenByThisCard` 目前只看 `context.TargetHoop.IsBroken`，沒有記錄「是否由本張牌剛剛擊破」。

落差：

- 需要「本張卡造成的事件結果」資料，例如本張卡是否造成傷害、是否擊破籃框、造成多少傷害。
- status 類 condition 已可檢查 Team / Card status，但尚未涵蓋 Player status。

建議：

- 先做 `EffectEventContext` 或在 `TurnContext` 中加入本次效果事件資訊。
- PlayerRuntime / Player target 第一版已完成，status condition 已可檢查球員狀態。

### TargetSide / TargetKind

文件定義：

- Self / Opponent / Both。
- Team / Player / Card / Hoop / Zone。

程式現況：

- `Self`、`Opponent` 在 Team 與 Hoop 目標上有基本支援。
- Card target 目前主要支援自己這邊的手牌、牌庫、棄牌堆、Reserved、OutsideGame。
- `Both` 目前 validation 會警告，runtime 尚未完整支援。
- `Player`、`Zone` 類目標在資料 enum 中存在，但 resolver 尚未完整實作。

落差：

- 目前沒有完整的「雙方目標集合」解析。
- 目前沒有可選玩家目標或球員狀態目標。

建議：

- 下一階段實作 Status 前，先補 `Player` target 與 `Both` target 的解析模型。

### TargetSelector

文件定義：

- ThisCard
- NextCard
- Random
- All
- PlayerChoice
- HighestAttackPlayer
- LowestCostCard
- CardsInZone
- OwnerPlayer
- ActingTeam
- OpponentTeam

程式現況：

- 已支援：`ThisCard`、`Random`、`All`、`LowestCostCard`、`CardsInZone`、`OwnerPlayer`、`HighestAttackPlayer`。
- `ActingTeam`、`OpponentTeam` 對 Team 目標間接可用。
- 尚未支援：`NextCard`、`PlayerChoice` 的完整 runtime 行為。

落差：

- `NextCard` 需要事件佇列或「下一張牌」監聽器。
- `PlayerChoice` 需要 UI 選擇流程。
- `HighestAttackPlayer`、`OwnerPlayer` 已有 Player target resolver 第一版。

建議：

- `PlayerChoice` 放到後面，因為會牽涉 BattleUI 與玩家互動狀態。

### Duration

文件定義：

- Instant
- CurrentPhase
- OwnAttackPhases
- OwnDefensePhases
- AnyOwnPhases
- FullRounds
- ThisGame
- UntilUsed
- UntilTriggered

程式現況：

- `Instant` 已可用。
- `CurrentPhase` 已可透過 `StatusRuntime` 在 phase end 倒數移除。
- `OwnAttackPhases`、`OwnDefensePhases`、`AnyOwnPhases` 已可透過 status 在 phase end 倒數。
- `UntilTriggered` 已可在部分 modifier 被使用後移除，例如傷害/護盾相關 modifier。
- 舊卡牌仍有一部分使用 `TurnContext` 暫存數值，例如本階段增傷、下一張攻擊、下一次受擊減傷、手牌降費。

落差：

- `ThisGame`、`FullRounds` 尚未接上統一倒數點。
- `UntilUsed` 尚未定義與卡牌使用事件的消耗規則。
- 現有舊式 `TurnContext` 暫存 modifier 尚未全部搬到 status。

建議：

- 下一步應補 `FullRounds` / `ThisGame` / `UntilUsed` 的 tick 與消耗規則。
- 逐步把舊式 `TurnContext` 暫存 modifier 搬到 Status / Modifier。

## 尚未完整支援

### Status / Modifier 系統

文件已預留：

- `TargetHasStatus`
- `StatusStackAtLeast`
- `ApplyTeamStatus`
- `ApplyPlayerStatus`
- `ApplyCardStatus`
- `ApplyModifier`
- `ClearStatus`

程式現況：

- 已新增 `StatusRuntime`、`StatusModifierRuntime`、`StatusContainer`。
- `TeamRuntime`、`CardRuntime` 已可持有 status。
- 已支援 `ApplyTeamStatus`、`ApplyCardStatus`、`ApplyModifier`、`ClearStatus`。
- 已支援 `TargetHasStatus`、`StatusStackAtLeast` 條件。
- 已支援 Team / Card status 的層數疊加與 phase end duration tick。
- 已支援部分 modifier：攻擊增傷、受擊減傷、護盾增幅、AP、Max AP、抽牌數、卡牌費用資料型別。
- 已新增 `PlayerRuntime`，`ApplyPlayerStatus` 已可把 status 掛到球員身上。

影響：

- 已可實作 Team / Card 層級的狀態，例如「全隊增傷到本階段結束」、「某張卡下一次攻擊增傷」。
- 已可支援 Player 層級的部分狀態，例如「某球員造成傷害增加」。
- `FullRounds` / `ThisGame` / `UntilUsed` 尚未完整接上。

建議優先度：已完成 Team / Card / Player 第一版，下一步補長期 duration 與更多 player modifier 類型。

### PlayCard / PlayRandomCards / CopyCard / RepeatDamage / ModifyShield

文件或 enum 中已有，但 runtime 尚未實作：

- `RepeatDamage`
- `ModifyShield`
- `CopyCard`
- `PlayCard`
- `PlayRandomCards`

目前 validation 會對這些 action 顯示「runtime 尚未支援」警告。

建議：

- `PlayRandomCards` 可與現有 Combo 行為整合。
- `CopyCard` 需要定義複製的是 CardData 還是 CardRuntime 暫時狀態。
- `ModifyShield` 需要先決定是修改現有 shield、未來 shield 產量，還是 shield multiplier。

### PlayerChoice

文件定義玩家可選目標。

程式現況：

- 尚未有選牌、選球員或選目標 UI。

影響：

- 暫時不能做「選一張手牌降費」、「選一名球員獲得狀態」這類技能。

建議：

- 等 PlayerChoice UI 流程完成後再做。

### Opponent Card Zone Target

文件範例有「敵方棄牌堆」。

程式現況：

- `TargetResolver.GetCardsInZone` 對 `TargetSide.Opponent` 直接回傳空集合。

影響：

- 無法對敵方手牌、牌庫、棄牌堆操作。
- 目前 AI 也沒有公開對方 hand 作為可被卡牌目標選取的資料。

建議：

- 先決定是否允許玩家干涉敵方牌庫。
- 若要支援，需要 `TurnContext` 提供 self/opponent deck 與 hand 的穩定參照。

## 程式已有但文件可以補充的行為

### Rebound 機制

目前程式有：

- 若防守成功，下一次自己進攻時額外獲得 Rebound 卡。
- Rebound 是 Item 卡。
- Rebound 消耗 0 AP，增加 1 AP，並有 Exhaust/消滅性質。

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
- 場景修復、卡面生成、字體/視覺工具。

文件可補：

- 新增卡牌後必跑 validation。
- 修改 V2 resolver 後必跑 smoke tests。

## 建議下一步順序

### 1. FullRounds / ThisGame / UntilUsed Duration

原因：

- Team / Card / Player status 第一版已完成。
- phase end tick 已有第一版。
- 跨 Round 與整場持續還需要戰鬥流程提供 tick 點。

建議內容：

- Round end tick。
- ThisGame 狀態保留到戰鬥結束。
- UntilUsed 與卡牌使用事件綁定。

### 2. Both Target Resolver

原因：

- `TargetSide.Both` 目前 validation 會警告。
- 部分全場型效果會需要雙方目標。

建議內容：

- `ResolveTeams`
- `ResolveCards` 支援雙方區域時的資料來源。
- 釐清是否允許影響敵方手牌或牌庫。

### 3. Effect Event Context

原因：

- `HoopBrokenByThisCard` 需要知道是否由本卡造成。
- 之後也會需要「本卡造成多少傷害」、「是否超殺」、「是否打到 HP」等條件。

建議內容：

- `EffectEventContext`
- `LastDamageDealt`
- `HoopBrokenByThisEffect`
- `TriggeredCard`

### 4. Action 補完

建議優先：

1. `PlayRandomCards`
2. `CopyCard`
3. `RepeatDamage`
4. `ModifyShield`
5. `ClearStatus`

### 5. PlayerChoice UI

原因：

- 需要 UI 流程。
- 牽涉玩家選擇、取消、卡牌暫停結算。

建議等前面資料模型穩定後再做。

## 目前不急著做

- 完整動畫系統。
- 完整卡牌美術。
- 聯機。
- 商店、養成、裝備。
- 複雜 AI 策略樹。

目前最重要的是先讓卡牌規則語言穩定，然後再大量設計卡牌。
