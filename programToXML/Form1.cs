using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace programToXML
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //Тип линии
        enum LineKind
        {
            Vertical = 0,
            Horizontal = 1,
            DiagonalUp = 2,
            DiagonalDown = 3
        }

        // способ определить
        private LineKind DetermineLineKind(int sX, int sY, int eX, int eY)
        {
            if (sY == eY) return LineKind.Horizontal;
            if (sX == eX) return LineKind.Vertical;
            return sY > eY ? LineKind.DiagonalDown : LineKind.DiagonalUp;
        }

        // Метод для превращения прямых углов в диагонали
        private void ConvertRightAnglesToDiagonals(XElement newXml, int increment)
        {
            var lines = newXml.Element("lines")?.Elements("line")?.ToList(); // Копия списка линий
            if (lines == null || lines.Count < 2) return;

            var linesToRemove = new HashSet<XElement>();
            var pointsToRemove = new HashSet<XElement>();
            var newLines = new List<XElement>();

            foreach (var line1 in lines)
            {
                if (linesToRemove.Contains(line1)) continue;

                var sX1 = int.Parse(line1.Attribute("sX")?.Value ?? "0");
                var sY1 = int.Parse(line1.Attribute("sY")?.Value ?? "0");
                var eX1 = int.Parse(line1.Attribute("eX")?.Value ?? "0");
                var eY1 = int.Parse(line1.Attribute("eY")?.Value ?? "0");

                var lineInfo1 = line1.Element("lineInfo");
                var lineType1 = lineInfo1?.Attribute("type")?.Value ?? "";
                var lineName1 = lineInfo1?.Attribute("name")?.Value ?? "";
                var specialization1 = lineInfo1?.Attribute("specialization")?.Value ?? "";
                var length1 = lineInfo1?.Attribute("length")?.Value ?? "";

                bool line1IsVertical = sX1 == eX1;
                bool line1IsHorizontal = sY1 == eY1;

                foreach (var line2 in lines)
                {
                    if (line1 == line2 || linesToRemove.Contains(line2)) continue;

                    var sX2 = int.Parse(line2.Attribute("sX")?.Value ?? "0");
                    var sY2 = int.Parse(line2.Attribute("sY")?.Value ?? "0");
                    var eX2 = int.Parse(line2.Attribute("eX")?.Value ?? "0");
                    var eY2 = int.Parse(line2.Attribute("eY")?.Value ?? "0");

                    bool line2IsVertical = sX2 == eX2;
                    bool line2IsHorizontal = sY2 == eY2;

                    // Проверяем, есть ли прямой угол (конец одной линии совпадает с началом другой)
                    if ((eX1 == sX2 && eY1 == sY2) && // Совпадение точки
                        ((line1IsVertical && line2IsHorizontal) || (line1IsHorizontal && line2IsVertical))) // Перпендикулярность
                    {

                        var kind = DetermineLineKind(sX1, sY1, eX2, eY2);

                        // Создаем диагональную линию
                        newLines.Add(new XElement("line",
                            new XAttribute("id", increment.ToString()),
                            new XAttribute("sX", sX1),
                            new XAttribute("sY", sY1),
                            new XAttribute("eX", eX2),
                            new XAttribute("eY", eY2),
                            new XAttribute("kind", (int)kind),
                            new XElement("lineInfo",
                        new XAttribute("type", lineType1),
                        new XAttribute("name", lineName1),
                        new XAttribute("specialization", specialization1),
                        new XAttribute("lengthInVagons", "0"),
                        new XAttribute("length", length1),
                        new XAttribute("park", ""),
                        new XAttribute("lengthLeft", "0"),
                        new XAttribute("nameLeft", ""),
                        new XAttribute("signalLeft", "3"),
                        new XAttribute("lengthRight", "0"),
                        new XAttribute("nameRight", ""),
                        new XAttribute("signalRight", "3")
                    )
                        ));

                        // Помечаем старые линии для удаления
                        linesToRemove.Add(line1);
                        linesToRemove.Add(line2);

                        increment++;

                        break; // Переходим к следующей линии
                    }
                }
            }

            // Найти точки, связанные с удаляемыми линиями
            var points = newXml.Element("points")?.Elements("point").ToList();
            if (points != null)
            {
                foreach (var line in linesToRemove)
                {
                    var sX = int.Parse(line.Attribute("sX")?.Value ?? "0");
                    var sY = int.Parse(line.Attribute("sY")?.Value ?? "0");
                    var eX = int.Parse(line.Attribute("eX")?.Value ?? "0");
                    var eY = int.Parse(line.Attribute("eY")?.Value ?? "0");

                    pointsToRemove.UnionWith(points.Where(p =>
                        (int.Parse(p.Attribute("X")?.Value ?? "0") == sX &&
                         int.Parse(p.Attribute("Y")?.Value ?? "0") == sY) ||
                        (int.Parse(p.Attribute("X")?.Value ?? "0") == eX &&
                         int.Parse(p.Attribute("Y")?.Value ?? "0") == eY)
                    ));
                }

                // Удалить точки
                foreach (var point in pointsToRemove)
                {
                    Debug.WriteLine(point);
                    point.Remove();
                }
            }

            // Удаляем старые линии
            foreach (var line in linesToRemove)
            {
                Debug.WriteLine(line);
                line.Remove();

            }

            // Добавляем новые линии
            foreach (var newLine in newLines)
            {
                newXml.Element("lines")?.Add(newLine);
            }
        }

        private void MergeLines(XElement newXml, int increment)
        {
            // Извлекаем список точек
            var points = newXml.Element("points")?.Elements("point").ToList();
            var lines = newXml.Element("lines")?.Elements("line").ToList();

            if (points == null || lines == null) return;

            var pointsToRemove = new HashSet<XElement>();
            var linesToRemove = new HashSet<XElement>();
            var newLines = new List<XElement>();

            foreach (var line1 in lines)
            {
                // Координаты конца первой линии
                var sX1 = int.Parse(line1.Attribute("sX")?.Value ?? "0");
                var sY1 = int.Parse(line1.Attribute("sY")?.Value ?? "0");
                var eX1 = int.Parse(line1.Attribute("eX")?.Value ?? "0");
                var eY1 = int.Parse(line1.Attribute("eY")?.Value ?? "0");

                var lineInfo1 = line1.Element("lineInfo");
                var lineType1 = lineInfo1?.Attribute("type")?.Value ?? "";
                var lineName1 = lineInfo1?.Attribute("name")?.Value ?? "";
                var specialization1 = lineInfo1?.Attribute("specialization")?.Value ?? "";
                var length1 = lineInfo1?.Attribute("length")?.Value ?? "";


                foreach (var line2 in lines)
                {
                    if (line1 == line2 || linesToRemove.Contains(line1) || linesToRemove.Contains(line2))
                    {
                        continue;
                    }

                    // Координаты начала и конца второй линии
                    var sX2 = int.Parse(line2.Attribute("sX")?.Value ?? "0");
                    var sY2 = int.Parse(line2.Attribute("sY")?.Value ?? "0");
                    var eX2 = int.Parse(line2.Attribute("eX")?.Value ?? "0");
                    var eY2 = int.Parse(line2.Attribute("eY")?.Value ?? "0");

                    var lineInfo2 = line1.Element("lineInfo");
                    var length2 = lineInfo1?.Attribute("length")?.Value ?? "";


                    // Проверяем наличие общей точки без имени
                    var connectingPoint = points.FirstOrDefault(point =>
                        int.Parse(point.Attribute("X")?.Value ?? "0") == eX1 &&
                        int.Parse(point.Attribute("Y")?.Value ?? "0") == eY1 &&
                        int.Parse(point.Attribute("X")?.Value ?? "0") == sX2 &&
                        int.Parse(point.Attribute("Y")?.Value ?? "0") == sY2 &&
                        string.IsNullOrEmpty(point.Element("pointInfo")?.Attribute("number")?.Value)
                    );

                    // Проверяем, находятся ли линии в одной плоскости (совпадают X или Y)
                    bool samePlane = (sY1 == eY1 && sY1 == sY2 && sY1 == eY2) || // Горизонтальная плоскость
                                     (sX1 == eX1 && sX1 == sX2 && sX1 == eX2);   // Вертикальная плоскость

                    if (connectingPoint != null && samePlane)
                    {
                        // Создаем объединенную линию
                        var newLine = new XElement("line",
                            new XAttribute("id", increment.ToString()),
                            new XAttribute("sX", sX1),
                            new XAttribute("sY", sY1),
                            new XAttribute("eX", eX2),
                            new XAttribute("eY", eY2),
                            new XAttribute("kind", (int)DetermineLineKind(sX1, sY1, eX2, eY2)),
                            new XElement("lineInfo",
                                new XAttribute("type", lineType1),
                                new XAttribute("name", lineName1),
                                new XAttribute("specialization", specialization1),
                                new XAttribute("lengthInVagons", "0"),
                                new XAttribute("length", length1 + length2),
                                new XAttribute("park", ""),
                                new XAttribute("lengthLeft", "0"),
                                new XAttribute("nameLeft", ""),
                                new XAttribute("signalLeft", "3"),
                                new XAttribute("lengthRight", "0"),
                                new XAttribute("nameRight", ""),
                                new XAttribute("signalRight", "3")
                            )
                        );

                        newLines.Add(newLine);

                        // Помечаем для удаления линии и точку
                        linesToRemove.Add(line1);
                        linesToRemove.Add(line2);
                        pointsToRemove.Add(connectingPoint);

                        increment++;
                        break;
                    }
                }
            }

            // Удаляем старые линии и точки
            foreach (var line in linesToRemove)
            {
                line.Remove();
            }

            foreach (var point in pointsToRemove)
            {
                point.Remove();
            }

            // Добавляем новые линии
            foreach (var newLine in newLines)
            {
                newXml.Element("lines")?.Add(newLine);
            }
        }

        // Обработчик кнопки выбора исходного файла
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Supported Files (*.xml;*.spg)|*.xml;*.spg|XML Files (*.xml)|*.xml|SPG Files (*.spg)|*.spg";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtSourceFile.Text = openFileDialog.FileName;
            }
        }


        // Обработчик кнопки "Выполнить" для преобразования XML
        private void btnExecute_Click(object sender, EventArgs e)
        {
            string sourceFile = txtSourceFile.Text;

            if (string.IsNullOrEmpty(sourceFile))
            {
                MessageBox.Show("Пожалуйста, выберите исходный файл.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {

                XDocument oldXml = null;


                if (Path.GetExtension(sourceFile).Equals(".spg", StringComparison.OrdinalIgnoreCase))
                {
                    // Открываем архив с использованием SharpCompress
                    using (var archive = ArchiveFactory.Open(sourceFile))
                    {
                        var entry = archive.Entries.FirstOrDefault(e => !e.IsDirectory);
                        if (entry != null)
                        {
                            using (var entryStream = entry.OpenEntryStream())
                            {
                                // Загружаем XML из потока
                                oldXml = XDocument.Load(entryStream);
                            }
                        }
                        else
                        {
                            MessageBox.Show("В архиве отсутствуют файлы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                // Обработка .xml файла
                else if (Path.GetExtension(sourceFile).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    oldXml = XDocument.Load(sourceFile);
                }
                else
                {
                    MessageBox.Show("Файл не является xml или spg!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Отладочный вывод для проверки всех элементов SchemaPoint
                var schemaPoints = oldXml.Root.Descendants("SchemaPoint").ToList();
                Debug.WriteLine($"Найдено {schemaPoints.Count} элементов SchemaPoint");

                foreach (var point in schemaPoints)
                {
                    Debug.WriteLine($"X: {point.Attribute("X")?.Value}, Y: {point.Attribute("Y")?.Value}");
                }

                // Найдем максимальные значения X и Y
                CultureInfo culture = CultureInfo.InvariantCulture;

                double maxX = schemaPoints
                                 .Select(p => double.TryParse(p.Attribute("X")?.Value, NumberStyles.Float, culture, out var x) ? x : 0)
                                 .Max();

                double maxY = schemaPoints
                                 .Select(p => double.TryParse(p.Attribute("Y")?.Value, NumberStyles.Float, culture, out var y) ? y : 0)
                                 .Max();

                Debug.WriteLine($"Максимальное значение X: {maxX}");
                Debug.WriteLine($"Максимальное значение Y: {maxY}");


                // Округляем значения до ближайшего большего целого для удобства
                int calculatedWidth = (int)Math.Ceiling(maxX);
                int calculatedHeight = (int)Math.Ceiling(maxY);


                // Выбираем максимальную координату
                double maxCoord = Math.Max(maxX, maxY);

                // Определяем масштаб
                double scale = 1;
                if (maxCoord >= 10000)
                {
                    scale = 100;
                }
                else if (maxCoord >= 1000)
                {
                    scale = 10;
                }


                XElement newXml = new XElement("StationMap",
                    new XAttribute("Step", "13"),
                    new XAttribute("Width", ((calculatedWidth / scale) + 10).ToString("0")),  // Ширина из координат
                    new XAttribute("Height", ((calculatedHeight / scale) + 10).ToString("0")), // Высота из координат
                    new XElement("points"),
                    new XElement("lines"),
                    new XElement("textCollection"),
                    new XElement("settings")
                );


                string title = "";

                XNamespace ns = oldXml.Root.GetDefaultNamespace(); // Получаем пространство имен корня
                XElement schemaElement = oldXml.Descendants(ns + "Schema").FirstOrDefault();
                if (schemaElement != null)
                {
                    title = schemaElement.Attribute("Title")?.Value ?? "";

                }
                else
                {
                    Debug.WriteLine("Element 'Schema' not found.");
                }

                newXml.Element("textCollection").Add(
                   new XElement("text",
                   new XAttribute("location_X", "5"),
                   new XAttribute("location_Y", "2"),
                   new XAttribute("size_W", "8"),
                   new XAttribute("size_H", "2"),
                   new XAttribute("text", title),
                   new XAttribute("alignment", "2"),
                   new XAttribute("fontFamilyName", "Microsoft Sans Serif"),
                   new XAttribute("fontStyle", "100"),
                   new XAttribute("fontSize", "15"),
                   new XAttribute("color", "-16777216"),
                   new XAttribute("angle", "0")
                   )
              );


                /*
                 * 
                 * Создание точек
                 * 
                 */


                int increment = 1;

                var switchsElement = oldXml.Root.Element("Switchs");
                var schemaPointsElement = oldXml.Root.Element("Points");

                if (schemaPointsElement != null)
                {
                    foreach (var schemaPoint in schemaPointsElement.Elements("SchemaPoint"))
                    {
                        string pointId = schemaPoint.Attribute("Id")?.Value;
                        if (string.IsNullOrEmpty(pointId)) continue;

                        string x = schemaPoint.Attribute("X")?.Value;
                        string y = schemaPoint.Attribute("Y")?.Value;

                        // Преобразуем координаты с использованием масштаба
                        string shortX = !string.IsNullOrEmpty(x) && double.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out var xCoord)
                            ? (Math.Round(xCoord / scale)).ToString("0")
                            : x;

                        string shortY = !string.IsNullOrEmpty(y) && double.TryParse(y, NumberStyles.Any, CultureInfo.InvariantCulture, out var yCoord)
                            ? (Math.Round(yCoord / scale)).ToString("0")
                            : y;

                        // Ищем, связан ли SchemaPoint с Switch
                        string switchName = "";
                        if (switchsElement != null)
                        {
                            foreach (var oneSwitch in switchsElement.Elements("Switch"))
                            {
                                var switchPoint = oneSwitch.Element("Point");
                                if (switchPoint == null) continue;

                                string switchPointId = switchPoint.Attribute("Id")?.Value;
                                if (pointId == switchPointId)
                                {
                                    switchName = oneSwitch.Attribute("Name")?.Value ?? "";
                                    break;
                                }
                            }
                        }

                        // Добавляем точку в новый XML
                        newXml.Element("points").Add(
                            new XElement("point",
                                new XAttribute("id", increment.ToString()),
                                new XAttribute("X", Convert.ToInt32(shortX)),
                                new XAttribute("Y", Convert.ToInt32(shortY)),
                                new XElement("pointInfo",
                                    new XAttribute("number", switchName), // Присваиваем имя или оставляем пустым
                                    new XAttribute("type", "2"),
                                    new XAttribute("textPosition", "3"),
                                    new XAttribute("gorlovina", "")
                                )
                            )
                        );

                        // Увеличиваем инкремент
                        increment++;
                    }
                }


                /*
                 * 
                 * Создание линий
                 * 
                 */

                var editorTracksElement = oldXml.Root.Element("EditorTracks");
                var sectionsElement = oldXml.Root.Element("Sections");

                if (editorTracksElement != null && sectionsElement != null)
                {
                    increment = 1;

                    foreach (var editorTrack in editorTracksElement.Elements("EditorTrack"))
                    {

                        string editorTrackNumber = "";
                        string editorTrackType = editorTrack.Attribute("Type")?.Value;

                        // Проверяем тип и присваиваем Number только если тип "Station" или "Main"
                        if (editorTrackType == "Station" || editorTrackType == "Main")
                        {
                            editorTrackNumber = editorTrack.Attribute("Number")?.Value ?? "";
                        }

                        var trackSections = editorTrack.Element("Sections");
                        if (trackSections == null) continue;

                        // Собираем длины секций, относящихся к текущей группе EditorTrack
                        var sectionsInTrack = trackSections.Elements("Section")
                            .Select(s => s.Attribute("Guid")?.Value)
                            .Where(guid => !string.IsNullOrEmpty(guid))
                            .Select(guid => sectionsElement.Elements("Section")
                                .FirstOrDefault(sec => sec.Attribute("Guid")?.Value == guid))
                            .Where(sec => sec != null)
                            .Select(sec => new
                            {
                                SectionElement = sec,
                                Length = double.TryParse(sec.Attribute("Length")?.Value, out var len) ? len : 0
                            })
                            .ToList();

                        // Находим самую длинную секцию в текущей группе
                        var longestSection = sectionsInTrack
                            .OrderByDescending(s => s.Length)
                            .FirstOrDefault();

                        if (longestSection != null)
                        {
                            Console.WriteLine($"Самая длинная секция: {longestSection.SectionElement} с длиной {longestSection.Length}");
                        }

                        // Остальная обработка для секций
                        foreach (var sectionData in sectionsInTrack)
                        {
                            var section = sectionData.SectionElement;
                            var startElement = section.Element("Start");
                            var endElement = section.Element("End");

                            if (startElement != null && endElement != null)
                            {
                                var startPoint = oldXml.Root.Descendants("SchemaPoint")
                                    .FirstOrDefault(p => p.Attribute("Id")?.Value == startElement.Attribute("Id")?.Value);
                                var endPoint = oldXml.Root.Descendants("SchemaPoint")
                                    .FirstOrDefault(p => p.Attribute("Id")?.Value == endElement.Attribute("Id")?.Value);

                                if (startPoint != null && endPoint != null)
                                {
                                    string startX = startPoint.Attribute("X")?.Value;
                                    string startY = startPoint.Attribute("Y")?.Value;
                                    string endX = endPoint.Attribute("X")?.Value;
                                    string endY = endPoint.Attribute("Y")?.Value;

                                    double sX = !string.IsNullOrEmpty(startX) && double.TryParse(startX, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedSX) ? parsedSX : 0;
                                    double sY = !string.IsNullOrEmpty(startY) && double.TryParse(startY, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedSY) ? parsedSY : 0;
                                    double eX = !string.IsNullOrEmpty(endX) && double.TryParse(endX, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedEX) ? parsedEX : 0;
                                    double eY = !string.IsNullOrEmpty(endY) && double.TryParse(endY, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedEY) ? parsedEY : 0;

                                    if (sX > eX)
                                    {
                                        (sX, eX) = (eX, sX);
                                        (sY, eY) = (eY, sY);
                                    }


                                    int shortStartX = (int)Math.Round(sX / scale);
                                    int shortStartY = (int)Math.Round(sY / scale);
                                    int shortEndX = (int)Math.Round(eX / scale);
                                    int shortEndY = (int)Math.Round(eY / scale);


                                    string length = section.Attribute("Length")?.Value ?? "0"; // Если длина не задана, берем 0


                                    string lineName = string.Empty;
                                    bool isMain = false;
                                    string specialization = "17";
                                    int lineType = 1;

                                    if (nameCheckBox.Checked)
                                    {

                                        // Присваиваем имя только для самой длинной секции
                                        lineName = section.Attribute("Guid")?.Value == longestSection?.SectionElement.Attribute("Guid")?.Value ? editorTrackNumber : "";

                                        // Получение значения IsMain
                                        isMain = section.Attribute("IsMain")?.Value == "true";

                                        // Определение значения specialization
                                        specialization = isMain ? "15" : string.IsNullOrEmpty(lineName) ? "17" : "2";

                                        lineType = !string.IsNullOrEmpty(lineName) ? 2 : 1;

                                    }

                                    // Определяем значение kind
                                    var kind = DetermineLineKind(shortStartX, shortStartY, shortEndX, shortEndY);

                                    newXml.Element("lines").Add(
                                        new XElement("line",
                                            new XAttribute("id", increment.ToString()), // Используем инкремент для линий
                                            new XAttribute("sX", shortStartX), // Преобразуем в целое число
                                            new XAttribute("sY", shortStartY), // Преобразуем в целое число
                                            new XAttribute("eX", shortEndX), // Преобразуем в целое число
                                            new XAttribute("eY", shortEndY), // Преобразуем в целое число
                                            new XAttribute("kind", (int)kind),
                                            new XElement("lineInfo",
                                                new XAttribute("type", lineType),
                                                new XAttribute("name", lineName),
                                                new XAttribute("specialization", specialization),
                                                new XAttribute("lengthInVagons", "0"),
                                                new XAttribute("length", length),
                                                new XAttribute("park", ""),
                                                new XAttribute("lengthLeft", "0"),
                                                new XAttribute("nameLeft", ""),
                                                new XAttribute("signalLeft", "3"),
                                                new XAttribute("lengthRight", "0"),
                                                new XAttribute("nameRight", ""),
                                                new XAttribute("signalRight", "3")
                                            )
                                        )
                                    );

                                    increment++;
                                }
                            }
                        }
                    }

                    // Применение метода для превращения прямых углов в диагонали
                    ConvertRightAnglesToDiagonals(newXml, increment);
                    MergeLines(newXml, increment);

                    // Определяем путь для нового файла
                    string destinationFile = Path.Combine(
                        Path.GetDirectoryName(sourceFile),
                        $"{Path.GetFileNameWithoutExtension(sourceFile)}-new.xml"
                    );
                    newXml.Save(destinationFile);

                    MessageBox.Show($"Преобразование завершено. Файл сохранен в: {destinationFile}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке файлов: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExecute_Click_1(object sender, EventArgs e)
        {

        }
    }

}