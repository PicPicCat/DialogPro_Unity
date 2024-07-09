namespace DialogPro.UI
{
    /// <summary>
    /// 注解中函数支持的接口 
    /// </summary>
    public interface INoteFunction
    {
        /// <summary>
        /// 调用的名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 获取注解消费
        /// </summary>
        /// <param name="dialogPrinter">所属打印器</param>
        /// <param name="element">包含改注解的元素</param>
        /// <returns>打印或调用该函数所需要的增量（等效字符长）</returns>
        float Cost(DialogPrinter dialogPrinter, PrintElement element);

        /// <summary>
        /// 处理注解
        /// </summary>
        /// <param name="dialogPrinter">所属打印器</param>
        /// <param name="element">包含改注解的元素</param>
        /// <returns>增加的字符串（将拼接与当前打印字符的尾部）</returns>
        void Handle(DialogPrinter dialogPrinter, PrintElement element);
    }
}