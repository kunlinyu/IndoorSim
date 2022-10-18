# version list

## V0.8.3
* [IndoorSim-WebGL-V0.8.3.cd4ba36](./IndoorSim-WebGL-V0.8.3.cd4ba36)
* [IndoorSim-WebGL-dev-V0.8.3.cd4ba36](./IndoorSim-WebGL-dev-V0.8.3.cd4ba36)
* [IndoorSim-StandaloneLinux64-V0.8.3.cd4ba36.tar.gz](./IndoorSim-StandaloneLinux64-V0.8.3.cd4ba36.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.8.3.cd4ba36.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.8.3.cd4ba36.tar.gz)

### DateTime
2022-10-18 14:18:06 +08

### ChangeLog
1. 解决了一些windows兼容问题
2. 将最新的Newtonsoft.Json.Schema源代码移至项目内
3. 修复错误了schema hash历史
4. 添加Split工具用于拆分货架
5. 修复拆分边时navigable错误的bug
6. 切换滚轮的缩放视角方向
7. 减小点和边的吸附范围

### Schema
* [schema](./schema/0.8.3/schema.json)(3AC0BED35318C7E853CAFCBCA198537F)

---

## V0.8.2
* [IndoorSim-WebGL-V0.8.2.2094f58](./IndoorSim-WebGL-V0.8.2.2094f58)
* [IndoorSim-WebGL-dev-V0.8.2.2094f58](./IndoorSim-WebGL-dev-V0.8.2.2094f58)
* [IndoorSim-StandaloneLinux64-V0.8.2.2094f58.tar.gz](./IndoorSim-StandaloneLinux64-V0.8.2.2094f58.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.8.2.2094f58.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.8.2.2094f58.tar.gz)

### DateTime
2022-10-12 19:58:17 +08

### ChangeLog
1. 修复了使用键盘删除边时的bug
2. 放宽schema检查的限制
3. 增加撤销重做的日至
4. 允许POI拖动到graph附近0.1米范围内

### Schema
* [schema](./schema/0.8.2/schema.json)(3AC0BED35318C7E853CAFCBCA198537F)

---

## V0.8.1

* [IndoorSim-WebGL-V0.8.1.55815fb](./IndoorSim-WebGL-V0.8.1.55815fb)
* [IndoorSim-WebGL-dev-V0.8.1.55815fb](./IndoorSim-WebGL-dev-V0.8.1.55815fb)
* [IndoorSim-StandaloneLinux64-V0.8.1.55815fb.tar.gz](./IndoorSim-StandaloneLinux64-V0.8.1.55815fb.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.8.1.55815fb.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.8.1.55815fb.tar.gz)

### DateTime
2022-10-10 13:17:27 +08

### ChangeLog

1. 修复了id工具输入id时有重复无法检查出来的bug
2. 更新了schema

### Schema
* [schema](./schema/0.8.1/schema.json)(3AC0BED35318C7E853CAFCBCA198537F)

---

## V0.8.0
* [IndoorSim-WebGL-V0.8.0.b4851a4](./IndoorSim-WebGL-V0.8.0.b4851a4)
* [IndoorSim-WebGL-dev-V0.8.0.b4851a4](./IndoorSim-WebGL-dev-V0.8.0.b4851a4)
* [IndoorSim-StandaloneLinux64-V0.8.0.b4851a4.tar.gz](./IndoorSim-StandaloneLinux64-V0.8.0.b4851a4.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.8.0.b4851a4.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.8.0.b4851a4.tar.gz)

### DateTime
2022-09-29 18:58:55 +08

### ChangeLog
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

## V0.7.1
* [IndoorSim-WebGL-V0.7.1.f352b80](./IndoorSim-WebGL-V0.7.1.f352b80)
* [IndoorSim-WebGL-dev-V0.7.1.f352b80](./IndoorSim-WebGL-dev-V0.7.1.f352b80)
* [IndoorSim-StandaloneLinux64-V0.7.1.f352b80.tar.gz](./IndoorSim-StandaloneLinux64-V0.7.1.f352b80.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.7.1.f352b80.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.7.1.f352b80.tar.gz)

### DateTime
2022-09-26 14:16:50 +08

### ChangeLog
1. 调整视角上下限
2. 调整POI的大小和缩放逻辑
3. 调整POI的颜色，删掉MAIN POI
4. 把软件版本写入文件中
5. 使用Newtonsoft.Json.Schema计算文件的Schema的哈希值并写入文件
6. 修复Agent数量非常多时Reset卡顿的bug
7. 修复Graph显示异常的bug
8. 调整保存文件时自动添加文件名后缀的策略
9. LineString工具点击边直接拆分
10. 添加ESC键作为鼠标右键的替代

---

## V0.7.0
* [IndoorSim-WebGL-V0.7.0.caf210d](./IndoorSim-WebGL-V0.7.0.caf210d)
* [IndoorSim-WebGL-dev-V0.7.0.caf210d](./IndoorSim-WebGL-dev-V0.7.0.caf210d)
* [IndoorSim-StandaloneLinux64-V0.7.0.caf210d.tar.gz](./IndoorSim-StandaloneLinux64-V0.7.0.caf210d.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.7.0.caf210d.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.7.0.caf210d.tar.gz)

### DateTime
2022-09-21 09:46:22 +08

### ChangeLog
1. 导出binlocations.json
2. 修复binlocations中导出非PICKING点的bug
3. 隐藏hierarchy与assets面板
4. 调整边通过方向标识的大小和颜色

---

## V0.6.0
* [IndoorSim-WebGL-V0.6.0.a0b37e2](./IndoorSim-WebGL-V0.6.0.a0b37e2)
* [IndoorSim-WebGL-dev-V0.6.0.a0b37e2](./IndoorSim-WebGL-dev-V0.6.0.a0b37e2)
* [IndoorSim-StandaloneLinux64-V0.6.0.a0b37e2.tar.gz](./IndoorSim-StandaloneLinux64-V0.6.0.a0b37e2.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.6.0.a0b37e2.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.6.0.a0b37e2.tar.gz)

### DateTime
2022-09-19 17:02:42 +08

### ChangeLog
1. 添加文件导出面板
2. 添加 location.yaml 文件导出工具
3. 添加 location.yaml 导出工具对排队点的支持
4. 添加将完整文件嵌如导出文件的接口
5. 修复快速绘制工具频繁向UI界面更新数据的性能缺陷
6. 修复在另一线程序列化导致读写冲突的bug

---

## V0.5.0
* [IndoorSim-WebGL-V0.5.0.78b5fd5](./IndoorSim-WebGL-V0.5.0.78b5fd5)
* [IndoorSim-WebGL-dev-V0.5.0.78b5fd5](./IndoorSim-WebGL-dev-V0.5.0.78b5fd5)
* [IndoorSim-StandaloneLinux64-V0.5.0.78b5fd5.tar.gz](./IndoorSim-StandaloneLinux64-V0.5.0.78b5fd5.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.5.0.78b5fd5.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.5.0.78b5fd5.tar.gz)

### DateTime
2022-09-14 14:19:11 +08

### ChangeLog
1. 添加IndoorFeatures以兼容IndoorGML2.0
2. 修改部分class的字段名以兼容IndoorGML2.0
3. 增加部分字段以兼容IndoorGML2.0
4. 添加多layer支持，并默认仅创建一个layer

---

## V0.4.0
* [IndoorSim-WebGL-V0.4.0.bf439d9](./IndoorSim-WebGL-V0.4.0.bf439d9)
* [IndoorSim-WebGL-dev-V0.4.0.bf439d9](./IndoorSim-WebGL-dev-V0.4.0.bf439d9)
* [IndoorSim-StandaloneLinux64-V0.4.0.bf439d9.tar.gz](./IndoorSim-StandaloneLinux64-V0.4.0.bf439d9.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.4.0.bf439d9.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.4.0.bf439d9.tar.gz)

### DateTime
2022-09-05 18:47:14 +08

### ChangeLog
1. 优化RELEASE页面效果
2. 添加POI创建面板
3. 根据POI面板创建不同类型的POI
4. 使用鼠标中键调整POI的交互逻辑
5. 调整POI的结构与PlanResult保持一致
6. 支持抵达POI前的队列，且支持不连续的队列
7. 打开与关闭显示POI

---

## V0.3.0
* [IndoorSim-WebGL-V0.3.0.e3638b6](./IndoorSim-WebGL-V0.3.0.e3638b6)
* [IndoorSim-WebGL-dev-V0.3.0.e3638b6](./IndoorSim-WebGL-dev-V0.3.0.e3638b6)
* [IndoorSim-StandaloneLinux64-V0.3.0.e3638b6.tar.gz](./IndoorSim-StandaloneLinux64-V0.3.0.e3638b6.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.3.0.e3638b6.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.3.0.e3638b6.tar.gz)

### DateTime
2022-08-23 15:51:15 +08

### ChangeLog
1. 优化事件队列和反序列化效率
2. 调整rine的显示效果
3. 生成和显示对偶网络
4. 将POI吸附到对偶网络上

---

## V0.2.3
* [IndoorSim-WebGL-V0.2.3.3cedd9d](./IndoorSim-WebGL-V0.2.3.3cedd9d)
* [IndoorSim-WebGL-dev-V0.2.3.3cedd9d](./IndoorSim-WebGL-dev-V0.2.3.3cedd9d)
* [IndoorSim-StandaloneLinux64-V0.2.3.3cedd9d.tar.gz](./IndoorSim-StandaloneLinux64-V0.2.3.3cedd9d.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.2.3.3cedd9d.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.2.3.3cedd9d.tar.gz)

### DateTime
2022-08-18 10:47:11 +08

### ChangeLog
1. 优化POI工具操作
2. 拖拽POI时检查POI落点正确性
3. 增加操控视角的按键
4. 修复部分UI界面没有正确拒绝场景操作的bug
5. 增加XY坐标轴
6. 修复多边形内部洞被拖拽到外侧时的bug
7. 修复两个货架工具当货架宽度反向时的bug
8. 合并space时使用navigable属性更大的值
9. 修复了删除边导致space信息丢失而无法正确undo的bug

---

## V0.2.2

* [IndoorSim-WebGL-V0.2.2.9dbbecf](./IndoorSim-WebGL-V0.2.2.9dbbecf)
* [IndoorSim-WebGL-dev-V0.2.2.9dbbecf](./IndoorSim-WebGL-dev-V0.2.2.9dbbecf)
* [IndoorSim-StandaloneLinux64-V0.2.2.9dbbecf.tar.gz](./IndoorSim-StandaloneLinux64-V0.2.2.9dbbecf.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.2.2.9dbbecf.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.2.2.9dbbecf.tar.gz)

### ChangeLog
1. 增加了Camra最大高度上限
2. POI随高度变化大小
3. 导入地图时删除垃圾日至
4. 开始仿真是检查当前状态是否可仿真

---

## V0.2.1
* [IndoorSim-WebGL-V0.2.1.278cdfd](./IndoorSim-WebGL-V0.2.1.278cdfd)
* [IndoorSim-WebGL-dev-V0.2.1.278cdfd](./IndoorSim-WebGL-dev-V0.2.1.278cdfd)
* [IndoorSim-StandaloneLinux64-V0.2.1.278cdfd.tar.gz](./IndoorSim-StandaloneLinux64-V0.2.1.278cdfd.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.2.1.278cdfd.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.2.1.278cdfd.tar.gz)

### ChangeLog
1. 修复了一些bug
2. 修复了StandaloneLinux64的包结构问题
3. 优化了选取与拖动工具的展示效果
4. 使用LFS管理nupkg
5. 创建脚本，用batch mode构建项目
6. 使用MarkDown管理release页面
7. 更新了构建脚本和上传脚本
8. 把版本标签和鼠标提示从全局页面中拆分出来

---

## V0.2.0
* [IndoorSim-WebGL-V0.2.0.d2dc12a](./IndoorSim-WebGL-V0.2.0.d2dc12a)
* [IndoorSim-WebGL-dev-V0.2.0.d2dc12a](./IndoorSim-WebGL-dev-V0.2.0.d2dc12a)
* [IndoorSim-StandaloneLinux64-V0.2.0.d2dc12a.tar.gz](./IndoorSim-StandaloneLinux64-V0.2.0.d2dc12a.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.2.0.d2dc12a.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.2.0.d2dc12a.tar.gz)

### ChangeLog
1. 完善自动构建脚本
2. 更该POI与Space的引用关系
3. 实现POI的添加、删除、拖拽和对应的Redo、Undo
4. 实现POI的序列化和反序列化
5. 消除了一些警告

---

## V0.1.4

* [IndoorSim-WebGL-V0.1.4.57482c3](./IndoorSim-WebGL-V0.1.4.57482c3)
* [IndoorSim-WebGL-dev-V0.1.4.57482c3](./IndoorSim-WebGL-dev-V0.1.4.57482c3)
* [IndoorSim-StandaloneLinux64-V0.1.4.57482c3.tar.gz](./IndoorSim-StandaloneLinux64-V0.1.4.57482c3.tar.gz)
* [IndoorSim-StandaloneLinux64-dev-V0.1.4.57482c3.tar.gz](./IndoorSim-StandaloneLinux64-dev-V0.1.4.57482c3.tar.gz)

### ChangeLog
1. Refactor structure about UI
2. Add Build script to make CI better

---

## V0.1.3
* [IndoorSim-release-V0.1.3](./IndoorSim-release-V0.1.3)

---

## V0.1.2
* [IndoorSim-release-V0.1.2](./IndoorSim-release-V0.1.2)

---

## V0.1.1
* [IndoorSim-release-V0.1.1](./IndoorSim-release-V0.1.1)
