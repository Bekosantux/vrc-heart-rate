v0.2.0-a

パラメータの説明です
- HR  
心拍計からOSCで送信された心拍数（int）
- VRCHR/ManualHR_Float  
ラジアルパペットから手動で心拍数を制御するときに使用されます
- VRCHR/ForceManualControl_Bool  
OSC心拍数を上書きして手動心拍数を使います
- VRCHR/Local_FullHR_Float  
0~255の心拍数が0~1に正規化された値です（OSC、手動共通）
- VRCHR/Local_CyclePhase_Float  
鼓動の1周期の中での位相を0~1で表した値です。ループします。
- VRCHR/Local_Trigger
鼓動の1ループが始まる瞬間に1fのみ有効になります。