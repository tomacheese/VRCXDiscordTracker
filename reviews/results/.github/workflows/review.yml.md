# .github/workflows/review.yml レビュー結果

## ファイル概要

Pull Request作成時に自動的にレビュアーを割り当てるGitHub Actionsワークフロー。

## 評価項目

### 1. 設計・構造

#### 良い点

- シンプルで目的が明確
- 適切な権限設定（最小権限の原則）
- 専用のアクションを使用

#### 改善点

- エラーハンドリングがない
- 条件分岐やカスタマイズがない

### 2. トリガー設定

#### 現状

```yaml
on:
  pull_request_target:
    types: [opened, ready_for_review]
```

#### 考慮事項

- `pull_request_target`の使用はセキュリティリスクがある
- 通常のPRには`pull_request`イベントを使用すべき

#### 改善提案

```yaml
on:
  pull_request:
    types: [opened, ready_for_review, reopened]
  pull_request_target:
    types: [opened, ready_for_review]
    branches:
      - main
      - master
```

### 3. セキュリティ

#### 問題点

- `pull_request_target`はフォークからのPRでもシークレットにアクセス可能
- 悪意のあるコードの実行リスク

#### 改善提案

```yaml
jobs:
  add-reviews:
    runs-on: ubuntu-latest
    if: |
      github.event_name == 'pull_request' ||
      (github.event_name == 'pull_request_target' && 
       github.event.pull_request.head.repo.full_name == github.repository)
```

### 4. エラーハンドリング

#### 改善提案

```yaml
jobs:
  add-reviews:
    runs-on: ubuntu-latest
    steps:
      - name: Auto assign reviewers
        uses: kentaro-m/auto-assign-action@v2.0.0
        with:
          configuration-path: '.github/review-config.yml'
        continue-on-error: true
        
      - name: Notify on failure
        if: failure()
        uses: actions/github-script@v7
        with:
          script: |
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: '⚠️ 自動レビュアー割り当てに失敗しました。手動でレビュアーを設定してください。'
            })
```

### 5. 拡張性の向上

#### 推奨実装

```yaml
name: 'PR Review Management'

on:
  pull_request:
    types: [opened, ready_for_review, reopened, converted_to_draft]
  issue_comment:
    types: [created]

permissions:
  contents: read
  pull-requests: write
  issues: write

jobs:
  auto-assign:
    name: Auto assign reviewers
    runs-on: ubuntu-latest
    if: |
      github.event_name == 'pull_request' && 
      github.event.action != 'converted_to_draft'
    
    steps:
      - name: Check PR size
        id: pr-size
        uses: actions/github-script@v7
        with:
          script: |
            const pr = await github.rest.pulls.get({
              owner: context.repo.owner,
              repo: context.repo.repo,
              pull_number: context.issue.number
            });
            
            const additions = pr.data.additions;
            const deletions = pr.data.deletions;
            const total = additions + deletions;
            
            let size = 'small';
            if (total > 1000) size = 'large';
            else if (total > 500) size = 'medium';
            
            core.setOutput('size', size);
            core.setOutput('additions', additions);
            core.setOutput('deletions', deletions);

      - name: Add size label
        uses: actions/github-script@v7
        with:
          script: |
            const size = '${{ steps.pr-size.outputs.size }}';
            await github.rest.issues.addLabels({
              owner: context.repo.owner,
              repo: context.repo.repo,
              issue_number: context.issue.number,
              labels: [`size/${size}`]
            });

      - name: Auto assign reviewers
        uses: kentaro-m/auto-assign-action@v2.0.0
        with:
          configuration-path: '.github/review-config.yml'

      - name: Post review guidelines
        if: github.event.action == 'opened'
        uses: actions/github-script@v7
        with:
          script: |
            const body = `## レビューガイドライン
            
            このPRのレビューをお願いします。以下の点にご注意ください：
            
            - [ ] コードスタイルが.editorconfigに準拠している
            - [ ] 適切なエラーハンドリングが実装されている
            - [ ] セキュリティ上の懸念がない
            - [ ] パフォーマンスへの影響を考慮している
            - [ ] 必要に応じてテストが追加されている
            
            **PR サイズ**: ${{ steps.pr-size.outputs.size }} (+${{ steps.pr-size.outputs.additions }} -${{ steps.pr-size.outputs.deletions }})
            `;
            
            await github.rest.issues.createComment({
              owner: context.repo.owner,
              repo: context.repo.repo,
              issue_number: context.issue.number,
              body: body
            });

  handle-review-request:
    name: Handle review request comments
    runs-on: ubuntu-latest
    if: |
      github.event_name == 'issue_comment' &&
      github.event.issue.pull_request &&
      contains(github.event.comment.body, '/review')
    
    steps:
      - name: Parse review request
        uses: actions/github-script@v7
        with:
          script: |
            const comment = context.payload.comment.body;
            const requestMatch = comment.match(/\/review\s+@(\w+)/);
            
            if (requestMatch) {
              const reviewer = requestMatch[1];
              
              await github.rest.pulls.requestReviewers({
                owner: context.repo.owner,
                repo: context.repo.repo,
                pull_number: context.issue.number,
                reviewers: [reviewer]
              });
              
              await github.rest.reactions.createForIssueComment({
                owner: context.repo.owner,
                repo: context.repo.repo,
                comment_id: context.payload.comment.id,
                content: '+1'
              });
            }
```

### 6. ドキュメント化

READMEに追加推奨：

```markdown
## レビュープロセス

このリポジトリでは、Pull Request作成時に自動的にレビュアーが割り当てられます。

### 手動レビュー要求

PRコメントで以下のコマンドを使用できます：
- `/review @username` - 特定のユーザーにレビューを要求

### 自動割り当てルール

詳細は[.github/review-config.yml](.github/review-config.yml)を参照してください。
```

## 総合評価

基本的な自動レビュアー割り当て機能は実装されているが、エラーハンドリング、セキュリティ、拡張性の面で改善の余地がある。特に`pull_request_target`の使用は慎重に検討すべき。PR サイズの自動ラベリングやレビューガイドラインの自動投稿など、レビュープロセスを支援する追加機能の実装を推奨。