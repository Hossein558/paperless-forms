package ut.com.paperless.forms;

import org.junit.Test;
import com.paperless.forms.api.MyPluginComponent;
import com.paperless.forms.impl.MyPluginComponentImpl;

import static org.junit.Assert.assertEquals;

public class MyComponentUnitTest {
    @Test
    public void testMyName() {
        MyPluginComponent component = new MyPluginComponentImpl(null);
        assertEquals("names do not match!", "myComponent", component.getName());
    }
}