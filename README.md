# VRC Heart Rate Project

## これは何？
OSCでアバターに送信された心拍数を、簡単に利用できるようにする Modular Avatar プレハブと、プレハブ自動配置機能を提供します。  
心拍数を利用するギミックが複数あっても、重複せずに無駄のないようにプレハブを配置できます。

## どのように使う？
利用者側で気にすべきことは一切ありません。ただ、開発者が用意したプレハブをアバター内にドラッグ・アンド・ドロップするだけです！

### オプション
標準では、自動のOSCと、手動で心拍数を指定する機能がついています。  
手動調整が不要であれば、これらを無効化することで同期パラメーター数の削減(9bit)とメニューの簡素化が可能です！  
機能の切り替えは自動で追加される "VRCHeartRate_Core" プレハブから可能です。

### 開発者向け情報
すでにパラメーターは準備してあるので、すぐに心拍数を利用したギミックの開発に取りかかれます。  
詳しくは以下のリンクをご参照ください。  
https://bekosantux.github.io/ShopDoc/category/vrc-heart-rate/

## 前提アセット
このアセットの利用には、  
- VRC SDK Base  
- Non-Destructive Modular Framework (NDMF)

が必須です。あらかじめインストールしてください。

## インストール
VPMよりインストールできます。  
https://bekosantux.github.io/vpm-repos/

または、GithubのReleaseページから unitypackage をダウンロード可能です。

## 更新履歴
https://bekosantux.github.io/ShopDoc/VRCHeartRate/change-log/