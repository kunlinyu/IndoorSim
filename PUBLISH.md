# published version list

## V0.8.0
* [IndoorSim-WebGL-V0.8.0.b4851a4](./IndoorSim-WebGL-V0.8.0.b4851a4)
* [IndoorSim-WebGL-dev-V0.8.0.b4851a4](./IndoorSim-WebGL-dev-V0.8.0.b4851a4)
* [IndoorSim-StandaloneLinux64-V0.8.0.b4851a4.tar.gz](./IndoorSim-StandaloneLinux64-V0.8.0.b4851a4.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.8.0.b4851a4.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.8.0.b4851a4.tar.gz)

### DateTime
2022-09-30 12:55:00 +08

### Publish Note
本次发布的版本是 V0.8.0，是IndoorSim历时一年的准备，半年的开发，15个版本释放后的第一次正式发布。
本次发布主要包含的功能有：

- 建图模式
1. 基于增量版本Polygnizer的vertex、boundary、space编辑器
2. space、boundary自动生成庞加莱对偶图
3. 基于庞加莱对偶图的对偶超图的交通规则编辑
4. POI编辑
5. 排队队列区域编辑
6. 仿真环境（包括地图、机器人）编辑
7. 历史兼容格式导出

- 仿真模式
1. 仿真环境的时间控制（播放，家减速，暂停等）
2. 多种机器人外观模型和运动模型
3. Task、Action、Motion、signal四层机器人控制栈
4. 地图导航服务
5. TaskAllocator自动分配任务

- 孪生模式
1. Js-Unity-Bridge 实现Unity仿真环境与Web页面双向交互

本次发布的功能以建图模式为主，目的是替代调操作不便的建图工具，形成新的地图数据格式。
计划在下一个发布版本中提升建图模式的编辑效率，并完善更多孪生模式的功能。

### Recent ChangeLog
1. 在文件中记录软件版本
2. 在文件中记录本文件schame的哈希值
3. 避免页面刷新或关闭丢失数据
4. LineString工具拆分边时保持边的navigable和naviDir
5. LineString工具点击边直接拆分边
6. LineString工具允许最多两个交叉点，并自动拆分好交叉的边
7. 拖动POI时吸附在本spcace的graph上
8. 添加binLocation.csv导出工具
9. binLocation.csv导出时检查重复的编号
10. 使用Delete键删除选中的边
11. 增加ESC键实现与绝大多数时候鼠标右键相同的效果
12. 调整了一些显示效果

---

# all released versions
* [Release Page](./release.html)