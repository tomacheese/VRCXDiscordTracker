# .github/workflows/release.yml レビュー結果

## ファイル概要

自動リリースワークフロー。セマンティックバージョニングによる自動バージョン管理とGitHub Releaseの作成を行う。

## 評価項目

### 1. 設計・構造

#### 良い点

- 2段階のジョブ構成（バージョンバンプ → ビルド＆リリース）
- 並行実行制御（concurrency）の実装
- ジョブ間のデータ受け渡しが適切

#### 改善点

- エラーハンドリングが不十分
- ロールバック戦略がない

### 2. バージョン管理戦略

#### 良い点

- セマンティックバージョニングの採用
- コミットメッセージに基づく自動バージョニング
- カスタムリリースルールの詳細な定義

#### 改善点

1. バージョン更新のPowerShellスクリプトが複雑で脆弱
   ```yaml
   - name: Update version in project files
     run: |
       dotnet tool install -g dotnet-setversion
       setversion ${{ needs.bump-version.outputs.version }} VRCXDiscordTracker/VRCXDiscordTracker.csproj
       setversion ${{ needs.bump-version.outputs.version }} VRCXDiscordTracker.Updater/VRCXDiscordTracker.Updater.csproj
   ```

2. バージョン整合性チェックの欠如
   ```yaml
   - name: Verify version update
     run: |
       $version = Select-String -Path "**/*.csproj" -Pattern "<Version>(.*)</Version>" | ForEach-Object { $_.Matches.Groups[1].Value }
       if ($version -ne "${{ needs.bump-version.outputs.version }}") {
         throw "Version mismatch detected"
       }
   ```

### 3. ビルドプロセス

#### 問題点

- テストの実行がない
- ビルド成果物の検証がない
- パブリッシュディレクトリが不明確（`bin/Publish/*`）

#### 改善提案

```yaml
- name: Run tests before release
  run: dotnet test VRCXDiscordTracker.sln --configuration Release

- name: Verify build output
  run: |
    $requiredFiles = @(
      "VRCXDiscordTracker/bin/Release/net9.0-windows/win-x64/publish/VRCXDiscordTracker.exe",
      "VRCXDiscordTracker.Updater/bin/Release/net9.0/win-x64/publish/VRCXDiscordTracker.Updater.exe"
    )
    foreach ($file in $requiredFiles) {
      if (!(Test-Path $file)) {
        throw "Required file not found: $file"
      }
    }
```

### 4. アーティファクト管理

#### 問題点

- ZIPファイルの内容が不明確
- チェックサムやデジタル署名がない

#### 改善提案

```yaml
- name: Create release artifacts
  run: |
    # メインアプリケーション
    $mainFiles = @(
      "VRCXDiscordTracker/bin/Release/net9.0-windows/win-x64/publish/*"
    )
    Compress-Archive -Path $mainFiles -DestinationPath "VRCXDiscordTracker-${{ needs.bump-version.outputs.version }}.zip"
    
    # アップデーター
    $updaterFiles = @(
      "VRCXDiscordTracker.Updater/bin/Release/net9.0/win-x64/publish/*"
    )
    Compress-Archive -Path $updaterFiles -DestinationPath "VRCXDiscordTracker.Updater-${{ needs.bump-version.outputs.version }}.zip"
    
    # チェックサム生成
    Get-FileHash *.zip | Format-List | Out-File checksums.txt

- name: Sign artifacts (optional)
  if: env.SIGNING_CERTIFICATE != ''
  run: |
    # コード署名の実装
```

### 5. セキュリティ考慮事項

#### 改善点

1. シークレット管理
   ```yaml
   env:
     GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
     SIGNING_CERTIFICATE: ${{ secrets.SIGNING_CERTIFICATE }}
   ```

2. 権限の明示的な指定
   ```yaml
   permissions:
     contents: write
     packages: write
   ```

### 6. 完全な推奨ワークフロー

```yaml
name: Release

on:
  push:
    branches: [main, master]
  workflow_dispatch:
    inputs:
      bump_type:
        description: 'Version bump type'
        required: false
        default: 'auto'
        type: choice
        options:
          - auto
          - patch
          - minor
          - major

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: false

permissions:
  contents: write
  packages: write

jobs:
  bump-version:
    name: Determine version
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.tag-version.outputs.new_version }}
      tag: ${{ steps.tag-version.outputs.new_tag }}
      changelog: ${{ steps.tag-version.outputs.changelog }}

    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Bump version and push tag
        id: tag-version
        uses: mathieudutour/github-tag-action@v6.2
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          default_bump: ${{ github.event.inputs.bump_type || 'minor' }}
          custom_release_rules: |
            feat:minor:✨ Features
            fix:patch:🐛 Fixes
            docs:patch:📰 Docs
            chore:patch:🎨 Chore
            perf:patch:🎈 Performance
            refactor:patch:🧹 Refactoring
            build:patch:🔍 Build
            ci:patch:🔍 CI
            revert:patch:⏪ Revert
            style:patch:🧹 Style
            test:patch:👀 Test
            breaking:major:💥 Breaking Changes
          dry_run: ${{ github.event_name == 'pull_request' }}

  build-test-release:
    name: Build, Test and Release
    runs-on: windows-latest
    needs: bump-version
    if: needs.bump-version.outputs.version != ''

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}

      - name: Install version tool
        run: dotnet tool install -g dotnet-setversion

      - name: Update version
        run: |
          setversion ${{ needs.bump-version.outputs.version }} VRCXDiscordTracker/VRCXDiscordTracker.csproj
          setversion ${{ needs.bump-version.outputs.version }} VRCXDiscordTracker.Updater/VRCXDiscordTracker.Updater.csproj

      - name: Restore dependencies
        run: dotnet restore VRCXDiscordTracker.sln

      - name: Build solution
        run: dotnet build VRCXDiscordTracker.sln --configuration Release --no-restore

      - name: Run tests
        run: dotnet test VRCXDiscordTracker.sln --configuration Release --no-build --logger "trx"

      - name: Publish applications
        run: |
          dotnet publish VRCXDiscordTracker/VRCXDiscordTracker.csproj -p:PublishProfile=Publish
          dotnet publish VRCXDiscordTracker.Updater/VRCXDiscordTracker.Updater.csproj -p:PublishProfile=Publish

      - name: Create release artifacts
        run: |
          $version = "${{ needs.bump-version.outputs.version }}"
          
          # Create main app package
          $mainPath = "VRCXDiscordTracker/bin/Release/net9.0-windows/win-x64/publish"
          Compress-Archive -Path "$mainPath/*" -DestinationPath "VRCXDiscordTracker-$version.zip"
          
          # Create updater package
          $updaterPath = "VRCXDiscordTracker.Updater/bin/Release/net9.0/win-x64/publish"
          Compress-Archive -Path "$updaterPath/*" -DestinationPath "VRCXDiscordTracker.Updater-$version.zip"
          
          # Generate checksums
          Get-FileHash *.zip -Algorithm SHA256 | Format-Table -AutoSize | Out-File -FilePath checksums.txt
        shell: pwsh

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          tag_name: ${{ needs.bump-version.outputs.tag }}
          name: Release ${{ needs.bump-version.outputs.version }}
          body: |
            ${{ needs.bump-version.outputs.changelog }}
            
            ## Downloads
            - 📦 [VRCXDiscordTracker-${{ needs.bump-version.outputs.version }}.zip](https://github.com/${{ github.repository }}/releases/download/${{ needs.bump-version.outputs.tag }}/VRCXDiscordTracker-${{ needs.bump-version.outputs.version }}.zip)
            - 🔧 [VRCXDiscordTracker.Updater-${{ needs.bump-version.outputs.version }}.zip](https://github.com/${{ github.repository }}/releases/download/${{ needs.bump-version.outputs.tag }}/VRCXDiscordTracker.Updater-${{ needs.bump-version.outputs.version }}.zip)
            
            ## Checksums
            See `checksums.txt` for SHA256 hashes.
          files: |
            VRCXDiscordTracker-*.zip
            VRCXDiscordTracker.Updater-*.zip
            checksums.txt
          draft: false
          prerelease: false
          fail_on_unmatched_files: true
```

### 7. ロールバック戦略

```yaml
- name: Rollback on failure
  if: failure()
  run: |
    git push --delete origin ${{ needs.bump-version.outputs.tag }}
```

## 総合評価

自動リリースの基本機能は実装されているが、エンタープライズレベルの品質には達していない。主な改善点は、バージョン更新の堅牢性向上、テストの追加、アーティファクト管理の改善、エラーハンドリングの強化。特に、PowerShellスクリプトによる直接的なファイル編集は脆弱であり、専用ツールの使用を推奨。また、リリース前のテスト実行とチェックサムの生成は必須。