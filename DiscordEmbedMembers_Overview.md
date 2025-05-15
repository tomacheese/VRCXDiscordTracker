# DiscordEmbedMembers 機能概要

## クラス概要

`DiscordEmbedMembers` クラスは、VRChatインスタンスのメンバー情報および現在のユーザーのロケーション情報をもとに、Discord向けのリッチなEmbedメッセージを生成するためのユーティリティクラスです。  
主に、インスタンスの参加者リストを見やすく整形し、DiscordのEmbed制限（フィールド数・文字数など）を考慮した出力を行います。

### 主な用途

- VRChatインスタンスの現メンバー・過去メンバーをDiscordにEmbed形式で通知
- メンバーの状態（オーナー、自分、フレンド、その他）に応じた絵文字付与
- DiscordのEmbed制限に合わせたフィールド分割・省略処理

## 主なメソッドと役割

| メソッド名 | 概要 |
|------------|------|
| `GetEmbed()` | メンバー情報をもとにEmbedを構築し返す。複数のパターンでフィールドを生成し、Discordの制限に収まるものを選択。 |
| `GetBaseEmbed()` | タイトル・説明・著者・色など基本情報をセットしたEmbedBuilderを生成。 |
| `ValidateEmbed()` | EmbedBuilderがDiscordの制限内か検証。 |
| `SetFields()` | EmbedBuilderのフィールドを指定リストで置換。 |
| `Sanitize()` | テキスト内のアンダースコアをDiscord用にエスケープ。 |
| `FormatDateTime()` | Nullableな日時をローカルカルチャ形式の文字列に変換。 |
| `ReduceFields()` | フィールド数・文字数がEmbed制限を超える場合に段階的に削減。 |
| `GetMemberFields()` | メンバーリストからEmbedFieldBuilderリストを生成。 |
| `GetMembersString()` | メンバーリストを改行区切りの文字列に整形。 |
| `GetMemberEmoji()` | メンバーの状態に応じた絵文字（👑, 👤, ⭐️, ⬜️）を返す。 |

## 内部ロジックの特徴

- **EmbedFieldPattern**：現メンバー・過去メンバーの表示形式（フル/リンク省略/名前のみ）を複数パターン用意し、Embed制限に収まるまで段階的にフォールバック。
- **フィールド分割・省略**：メンバー数や文字数が多い場合、フィールドを分割・省略しつつ、最終的に「...」で省略表示。
- **アンダースコアのエスケープ**：DiscordのMarkdown仕様に合わせてユーザー名等のアンダースコアを自動エスケープ。
- **絵文字付与**：オーナー・自分・フレンド・その他で異なる絵文字を付与し、視認性を向上。

## パターンロジック

`GetEmbed()`メソッドでは、DiscordのEmbed制限（フィールド数・文字数）に収まるよう、  
現メンバー・過去メンバーの表示形式を複数パターンで試行し、最初に制限内に収まったパターンを採用します。

### パターン一覧

| パターン | 現メンバー表示 | 過去メンバー表示 | フィールド削減 |
|----------|----------------|------------------|----------------|
| 1        | フル           | フル             | しない         |
| 2        | フル           | リンク省略       | しない         |
| 3        | フル           | 名前のみ         | しない         |
| 4        | リンク省略     | 名前のみ         | しない         |
| 5        | 名前のみ       | 名前のみ         | しない         |
| 6        | 名前のみ       | 名前のみ         | する           |

- 各パターンでEmbedを構築し、`ValidateEmbed()`でDiscordの制限内か検証します。
- どのパターンでも収まらない場合は、フィールド数・内容をさらに削減（`ReduceFields()`）し、最終的に「...」で省略表示します。
- これにより、情報量と可読性を最大限維持しつつ、Discordの仕様に適合したEmbedを自動生成します。

## テストケース一覧

| テストクラス名                                      | テストメソッド名                                               | 概要（メソッド名から推測）                                      |
|-----------------------------------------------------|---------------------------------------------------------------|---------------------------------------------------------------|
| DiscordEmbedMembersInstanceTests                    | GetBaseEmbed_ValidInput_ReturnsEmbedBuilder                   | 正常な入力でEmbedBuilderが返る                                 |
| DiscordEmbedMembersInstanceTests                    | GetBaseEmbed_LocationIdInvalid_ThrowsFormatException          | locationIdが不正な場合にFormatExceptionが発生                  |
| DiscordEmbedMembersInstanceTests                    | GetMemberFields_EmptyMembers_ReturnsEmptyList                 | メンバーが空の場合に空リストが返る                             |
| DiscordEmbedMembersInstanceTests                    | GetMemberFields_FieldSplitByLength                            | フィールドが長さで分割される                                   |
| DiscordEmbedMembersInstanceTests                    | GetMembersString_TextFormatVariants                           | テキストフォーマットのバリエーション                            |
| DiscordEmbedMembersInstanceTests                    | GetMemberEmoji_AllPatterns                                    | 全パターンの絵文字取得                                         |
| DiscordEmbedMembersReduceFieldsTests                | ReduceFields_Over25Fields_TrimmedTo25                         | 25件を超えるフィールドが25件にトリムされる                     |
| DiscordEmbedMembersReduceFieldsTests                | ReduceFields_FieldCountOver25_TrimmedTo25                     | フィールド数が25件を超えた場合に25件にトリムされる             |
| DiscordEmbedMembersStaticTests                      | ValidateEmbed_ValidEmbed_ReturnsTrue                          | 有効なEmbedがtrueを返す                                        |
| DiscordEmbedMembersStaticTests                      | SetFields_FieldsAreSetCorrectly                               | フィールドが正しくセットされる                                 |
| DiscordEmbedMembersStaticTests                      | SetFields_EmptyList_ClearsFields                              | 空リストでフィールドがクリアされる                             |
| DiscordEmbedMembersStaticTests                      | Sanitize_Underscore_IsEscaped                                 | アンダースコアがエスケープされる                               |
| DiscordEmbedMembersStaticTests                      | Sanitize_Link_UnderscoreNotEscaped                            | リンク内のアンダースコアはエスケープされない                   |
| DiscordEmbedMembersStaticTests                      | Sanitize_Empty_ReturnsEmpty                                   | 空文字列で空が返る                                             |
| DiscordEmbedMembersStaticTests                      | FormatDateTime_Null_ReturnsEmpty                              | nullで空文字列が返る                                           |
| DiscordEmbedMembersStaticTests                      | FormatDateTime_ValidDateTime_ReturnsString                    | 有効な日時で文字列が返る                                       |
| DiscordEmbedMembersTests                            | GetEmbed_NoMembers_ReturnsValidEmbed                          | メンバーなしで有効なEmbedが返る                                |
| DiscordEmbedMembersTests                            | GetEmbed_OnlyCurrentSelfMember_ReturnsValidEmbed              | 現在の自分のみで有効なEmbedが返る                              |
| DiscordEmbedMembersTests                            | GetEmbed_OnlyCurrentMembers_MixedTypes_ReturnsValidEmbed      | 現在メンバー（混在型）のみで有効なEmbedが返る                  |
| DiscordEmbedMembersTests                            | GetEmbed_OnlyPastMember_ReturnsValidEmbed                     | 過去メンバーのみで有効なEmbedが返る                            |
| DiscordEmbedMembersTests                            | GetEmbed_OnlyPastMembers_ReturnsValidEmbed                    | 過去メンバーのみ（複数）で有効なEmbedが返る                    |
| DiscordEmbedMembersTests                            | GetEmbed_CurrentAndPastMembers_MixedTypes_ReturnsValidEmbed   | 現在・過去メンバー混在で有効なEmbedが返る                      |
| DiscordEmbedMembersTests                            | GetEmbed_MemberNameWithUnderscore_IsEscaped                   | メンバー名にアンダースコアが含まれる場合のエスケープ           |
| DiscordEmbedMembersTests                            | GetEmbed_MemberNameWithLink_IsNotEscaped                      | メンバー名にリンクが含まれる場合の非エスケープ                 |
| DiscordEmbedMembersTests                            | GetEmbed_MemberNameEmpty_ReturnsValidEmbed                    | メンバー名が空の場合でも有効なEmbedが返る                      |
| DiscordEmbedMembersTests                            | GetEmbed_LastLeaveAtNull_ReturnsValidEmbed                    | 最終退室時刻がnullでも有効なEmbedが返る                        |
| DiscordEmbedMembersTests                            | GetEmbed_LastLeaveAtLessThanJoinAt_ReturnsValidEmbed          | 退室時刻<入室時刻でも有効なEmbedが返る                         |
| DiscordEmbedMembersTests                            | GetEmbed_WorldNameOrIdOrLocationIdNull_ReturnsValidEmbed      | ワールド名/ID/locationIdがnullでも有効なEmbedが返る            |
| DiscordEmbedMembersTests                            | GetEmbed_LocationIdWithoutColon_ThrowsFormatException         | locationIdにコロンがない場合FormatException                    |
| DiscordEmbedMembersTests                            | GetEmbed_UserIdInCurrentMembers_ColorIsGreen                  | 現在メンバーにuserIdが含まれる場合は色が緑                     |
| DiscordEmbedMembersTests                            | GetEmbed_UserIdNotInCurrentMembers_ColorIsYellow              | 現在メンバーにuserIdが含まれない場合は色が黄                   |
| DiscordEmbedMembersTests                            | GetEmbed_MembersCount25_ReturnsValidEmbed                     | メンバー25人で有効なEmbedが返る                                |
| DiscordEmbedMembersTests                            | GetEmbed_MembersCount26_OneRemoved_ReturnsValidEmbed          | メンバー26人で1人除外され有効なEmbedが返る                     |
| DiscordEmbedMembersTests                            | GetEmbed_FieldValueExceedsMaxLength_IsSplit                   | フィールド値が最大長超過時に分割される                         |
| DiscordEmbedMembersTests                            | GetEmbed_FieldValueAtMaxLength_ReturnsValidEmbed              | フィールド値が最大長の時に有効なEmbedが返る                    |
| DiscordEmbedMembersTests                            | GetEmbed_LastFieldTooManyLines_IsTruncatedWithEllipsis        | 最後のフィールドが行数超過時に省略記号で切られる               |
| DiscordEmbedMembersTests                            | GetEmbed_LocationIdWithoutColon_ThrowsFormatException_Duplicate| locationIdにコロンがない場合FormatException（重複テスト）      |
| DiscordEmbedMembersTests                            | GetEmbed_MemberTextFormatPatterns_OutputIsCorrect             | メンバーテキストフォーマットパターンの出力が正しい             |
| DiscordEmbedMembersTests                            | GetEmbed_AllPatternsFail_ThrowsException                      | 全パターン失敗時に例外発生                                     |
